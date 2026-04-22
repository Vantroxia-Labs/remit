using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignBulkInvoice;

/// <summary>
/// Helper class to track signing statistics across batches
/// </summary>
internal class SigningStatistics
{
    public int TotalObjects { get; set; }
    public int SuccessfullySigned { get; set; }
    public int FailedSigning { get; set; }
    public Dictionary<string, string> FailedSigningDetails { get; set; } = new();

    public void RecordSuccess()
    {
        SuccessfullySigned++;
    }

    public void RecordFailure(string invoiceIdentifier, string reason)
    {
        FailedSigning++;
        FailedSigningDetails[invoiceIdentifier] = reason;
    }
}

public class SignBulkInvoiceCommandHandler(
 IApplicationDbContext context,
    ICurrentUserService currentUser,
    IAppProviderRouter appProviderRouter,
    ILogger<SignBulkInvoiceCommandHandler> logger)
    : IRequestHandler<SignBulkInvoiceCommand, SignBulkInvoiceResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<SignBulkInvoiceCommandHandler> _logger = logger;
    private readonly IAppProviderRouter _appProviderRouter = appProviderRouter;

    public async Task<SignBulkInvoiceResult> Handle(SignBulkInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (!IsUserAuthorized())
            return (SignBulkInvoiceResult)SignBulkInvoiceResult.AuthorizationError();

        var businessId = _currentUser.BusinessId!.Value;

        // Fetch all valid invoices with their business data in one query
        var invoices = await _context.Invoices
            .Include(i => i.Business)
            .Include(i => i.Party)
            .Include(i => i.InvoiceLine)
                .ThenInclude(il => il.BusinessItem)
                .ThenInclude(bi => bi!.ItemCategory)
            .Include(i => i.BillingReferences)
             .Include(i => i.AdditionalDocumentReferences)
               .Include(i => i.DispatchDocumentReference)
               .Include(i => i.ReceiptDocumentReference)
               .Include(i => i.OriginatorDocumentReference)
               .Include(i => i.ContractDocumentReference)
            .Where(i => request.InvoiceIds.Contains(i.Id)
                     && (i.InvoiceStatus == InvoiceStatus.VALIDATED || i.InvoiceStatus == InvoiceStatus.SIGNINGFAILED)
                     && i.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        if (!invoices.Any())
            return (SignBulkInvoiceResult)SignBulkInvoiceResult.NotFound("Invoice(s) have either been signed or do not exist");

        // Initialize signing statistics
        var stats = new SigningStatistics
        {
            TotalObjects = invoices.Count
        };

        // Group invoices by business to handle different credentials
        var invoicesByBusiness = invoices.GroupBy(i => i.Business);

        foreach (var businessGroup in invoicesByBusiness)
        {
            await ProcessBusinessInvoices(businessGroup.Key, businessGroup.ToList(), stats, cancellationToken);
        }

        // Return result with statistics
        return new SignBulkInvoiceResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = $"Signing completed. {stats.SuccessfullySigned} successful, {stats.FailedSigning} failed out of {stats.TotalObjects} invoices",
            TotalObjects = stats.TotalObjects,
            SuccessfullySigned = stats.SuccessfullySigned,
            FailedSigning = stats.FailedSigning,
            FailedSigningDetails = stats.FailedSigningDetails
        };
    }

    private async Task ProcessBusinessInvoices(Business business, List<Invoice> invoices, SigningStatistics stats, CancellationToken cancellationToken)
    {
        try
        {
            // Get configured provider adapter for this business
            var provider = await _appProviderRouter.GetProviderAsync(business.Id, cancellationToken);

            // Process invoices in batches for this business
            var batchSize = 10;
            var batches = invoices.Chunk(batchSize);

            foreach (var batch in batches)
            {
                await ProcessInvoiceBatch(batch, provider, stats, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider adapter for business {BusinessId}", business.Id);
            await MarkInvoicesAsFailed(invoices, "SIGNING FAILED: Unable to configure APP provider", stats, cancellationToken);
        }
    }

    private async Task ProcessInvoiceBatch(IEnumerable<Invoice> invoices, IAccessPointProviderClient provider, SigningStatistics stats, CancellationToken cancellationToken)
    {
        try
        {
            var signingTasks = invoices.Select(invoice =>
                SignSingleInvoice(invoice, provider, stats, cancellationToken));

            await Task.WhenAll(signingTasks);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var invoiceIds = string.Join(",", invoices.Select(i => i.Id));
            _logger.LogError(ex, "Failed to sign invoice batch: {InvoiceIds}", invoiceIds);

            // Mark failed invoices and record statistics
            foreach (var invoice in invoices)
            {
                var identifier = invoice.Irn?.Value ?? invoice.Id.ToString();
                stats.RecordFailure(identifier, ResponseMessages.INVOICE_SIGNING_FAILED);

                invoice.SetFIRSSubmissionResponseMessage(ResponseMessages.INVOICE_SIGNING_FAILED);
                invoice.UpdateStatus(InvoiceStatus.SIGNINGFAILED);
                _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.SIGNINGFAILED, ResponseMessages.INVOICE_SIGNING_FAILED));
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SignSingleInvoice(Invoice invoice, IAccessPointProviderClient provider, SigningStatistics stats, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Signing invoice {InvoiceId} with IRN {IRN} for business {BusinessId}",
            invoice.Id, invoice.Irn?.Value, invoice.BusinessId);

        var result = await provider.SignInvoiceAsync(invoice, cancellationToken);

        var identifier = invoice.Irn?.Value ?? invoice.Id.ToString();

        if (result.IsSuccess)
        {
            invoice.SetFIRSSubmissionResponseMessage(ResponseMessages.INVOICE_SIGNING_SUCCESSFUL);
            invoice.UpdateStatus(InvoiceStatus.SIGNED);
            _logger.LogInformation("Invoice {InvoiceId} successfully signed by FIRS", invoice.Id);
            _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus,
                ResponseMessages.INVOICE_SIGNING_SUCCESSFUL));

            // Record success
            stats.RecordSuccess();
        }
        else
        {
            var errorMessage = result.ErrorMessage ?? ResponseMessages.INVOICE_SIGNING_FAILED;
            _logger.LogWarning("Invoice {InvoiceId} signing failed: {ErrorCode} - {ErrorMessage}",
                invoice.Id, result.ErrorCode, errorMessage);
            invoice.SetFIRSSubmissionResponseMessage(errorMessage);
            invoice.UpdateStatus(InvoiceStatus.SIGNINGFAILED);
            _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus, errorMessage));

            // Record failure with details
            stats.RecordFailure(identifier, errorMessage);
        }
    }

    private async Task MarkInvoicesAsFailed(IEnumerable<Invoice> invoices, string message, SigningStatistics stats, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var invoice in invoices)
            {
                var identifier = invoice.Irn?.Value ?? invoice.Id.ToString();

                invoice.SetFIRSSubmissionResponseMessage(message);
                invoice.UpdateStatus(InvoiceStatus.SIGNINGFAILED);
                _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.SIGNINGFAILED, message));

                // Record failure in statistics
                stats.RecordFailure(identifier, message);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark invoices as failed");
            throw;
        }
    }

    private bool IsUserAuthorized() =>
        _currentUser.BusinessId.HasValue;
}
