using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.TransmitBulkInvoice;

/// <summary>
/// Helper class to track transmission statistics across batches
/// </summary>
internal class TransmissionStatistics
{
    public int TotalObjects { get; set; }
    public int SuccessfullyTransmitted { get; set; }
    public int FailedTransmission { get; set; }
    public Dictionary<string, string> FailedTransmissionDetails { get; set; } = new();

    public void RecordSuccess()
    {
        SuccessfullyTransmitted++;
    }

    public void RecordFailure(string invoiceIdentifier, string reason)
    {
        FailedTransmission++;
        FailedTransmissionDetails[invoiceIdentifier] = reason;
    }
}

public class TransmitBulkInvoiceCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IAppProviderRouter appProviderRouter,
    ILogger<TransmitBulkInvoiceCommandHandler> logger)
    : IRequestHandler<TransmitBulkInvoiceCommand, TransmitBulkInvoiceResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<TransmitBulkInvoiceCommandHandler> _logger = logger;
    private readonly IAppProviderRouter _appProviderRouter = appProviderRouter;

    public async Task<TransmitBulkInvoiceResult> Handle(TransmitBulkInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (!IsUserAuthorized())
            return (TransmitBulkInvoiceResult)TransmitBulkInvoiceResult.AuthorizationError();

        var businessId = _currentUser.BusinessId!.Value;

        // Fetch all valid invoices with their business data in one query
        var invoices = await _context.Invoices
            .Include(i => i.Business)
            .Include(i => i.Party)
            .Include(i => i.InvoiceLine)
                .ThenInclude(il => il.BusinessItem)
            .Where(i => request.InvoiceIds.Contains(i.Id)
                     && (i.InvoiceStatus == InvoiceStatus.SIGNED || i.InvoiceStatus == InvoiceStatus.TRANSMISSIONFAILED)
                     && i.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        if (!invoices.Any())
            return (TransmitBulkInvoiceResult)TransmitBulkInvoiceResult.NotFound("Invoice(s) have either been transmitted or do not exist");

        // Initialize transmission statistics
        var stats = new TransmissionStatistics
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
        return new TransmitBulkInvoiceResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = $"Transmission completed. {stats.SuccessfullyTransmitted} successful, {stats.FailedTransmission} failed out of {stats.TotalObjects} invoices",
            TotalObjects = stats.TotalObjects,
            SuccessfullyTransmitted = stats.SuccessfullyTransmitted,
            FailedTransmission = stats.FailedTransmission,
            FailedTransmissionDetails = stats.FailedTransmissionDetails
        };
    }

    private async Task ProcessBusinessInvoices(Business business, List<Invoice> invoices, TransmissionStatistics stats, CancellationToken cancellationToken)
    {
        try
        {
            // Resolve the APP provider once per business
            var appProvider = await _appProviderRouter.GetProviderAsync(business.Id, cancellationToken);

            var batchSize = 10;
            var batches = invoices.Chunk(batchSize);

            foreach (var batch in batches)
            {
                await ProcessInvoiceBatch(batch, business, appProvider, stats, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process business invoices for business {BusinessId}", business.Id);
            await MarkInvoicesAsFailed(invoices, "TRANSMISSION FAILED: Unable to process invoices", stats, cancellationToken);
        }
    }

    private async Task ProcessInvoiceBatch(
        IEnumerable<Invoice> invoices,
        Business business,
        IAccessPointProviderClient appProvider,
        TransmissionStatistics stats,
        CancellationToken cancellationToken)
    {
        try
        {
            var transmissionTasks = invoices.Select(invoice =>
                TransmitSingleInvoice(invoice, appProvider, stats, cancellationToken));

            await Task.WhenAll(transmissionTasks);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var invoiceIds = string.Join(",", invoices.Select(i => i.Id));
            _logger.LogError(ex, "Failed to transmit invoice batch for business {BusinessId}: {InvoiceIds}", business.Id, invoiceIds);

            foreach (var invoice in invoices)
            {
                var identifier = invoice.Irn?.Value ?? invoice.Id.ToString();
                stats.RecordFailure(identifier, ResponseMessages.INVOICE_TRANSMISSION_FAILED);
                invoice.SetFIRSSubmissionResponseMessage(ResponseMessages.INVOICE_TRANSMISSION_FAILED);
                invoice.UpdateStatus(InvoiceStatus.TRANSMISSIONFAILED);
                _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.TRANSMISSIONFAILED, ResponseMessages.INVOICE_TRANSMISSION_FAILED));
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task TransmitSingleInvoice(
        Invoice invoice,
        IAccessPointProviderClient appProvider,
        TransmissionStatistics stats,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Transmitting invoice {InvoiceId} with IRN {IRN} via {Provider}",
            invoice.Id, invoice.Irn?.Value, appProvider.ProviderCode);

        var identifier = invoice.Irn?.Value ?? invoice.Id.ToString();

        if (invoice.InvoiceKind == InvoiceKind.B2C)
        {
            _logger.LogWarning("Invoice {InvoiceId} is a B2C invoice and cannot be transmitted", invoice.Id);
            invoice.SetFIRSSubmissionResponseMessage(ResponseMessages.B2C_INVOICE_CANNOT_BE_TRANSMITTED);
            invoice.UpdateStatus(InvoiceStatus.TRANSMISSIONFAILED);
            _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.TRANSMISSIONFAILED, ResponseMessages.B2C_INVOICE_CANNOT_BE_TRANSMITTED));
            stats.RecordFailure(identifier, ResponseMessages.B2C_INVOICE_CANNOT_BE_TRANSMITTED);
            return;
        }

        if (appProvider.SupportsLookupTin)
        {
            var tinLookup = await appProvider.LookupTinAsync(invoice.Party.TaxIdentificationNumber.Value, cancellationToken);

            if (!tinLookup.IsSuccess || !tinLookup.IsUp)
            {
                var errorMessage = ResponseMessages.PARTY_NOT_FOUND;
                _logger.LogWarning("Invoice {InvoiceId} TIN lookup failed: {Message}", invoice.Id, tinLookup.ErrorMessage);
                invoice.SetFIRSSubmissionResponseMessage(errorMessage);
                invoice.UpdateStatus(InvoiceStatus.TRANSMISSIONFAILED);
                _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.TRANSMISSIONFAILED, errorMessage));
                stats.RecordFailure(identifier, errorMessage);
                return;
            }
        }

        var transmitResult = await appProvider.TransmitAsync(invoice.Irn!.Value, cancellationToken);

        if (transmitResult.IsSuccess)
        {
            invoice.SetFIRSSubmissionResponseMessage(ResponseMessages.INVOICE_TRANSMISSION_SUCCESSFUL);
            invoice.UpdateStatus(InvoiceStatus.TRANSMITTED);
            _logger.LogInformation("Invoice {InvoiceId} successfully transmitted via {Provider}", invoice.Id, appProvider.ProviderCode);
            _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus, ResponseMessages.INVOICE_TRANSMISSION_SUCCESSFUL));
            stats.RecordSuccess();
        }
        else
        {
            var errorMessage = transmitResult.ErrorMessage ?? ResponseMessages.INVOICE_TRANSMISSION_FAILED;
            _logger.LogWarning("Invoice {InvoiceId} transmission failed via {Provider}: {Message}", invoice.Id, appProvider.ProviderCode, errorMessage);
            invoice.SetFIRSSubmissionResponseMessage(errorMessage);
            invoice.UpdateStatus(InvoiceStatus.TRANSMISSIONFAILED);
            _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.TRANSMISSIONFAILED, errorMessage));
            stats.RecordFailure(identifier, errorMessage);
        }
    }

    private async Task MarkInvoicesAsFailed(IEnumerable<Invoice> invoices, string message, TransmissionStatistics stats, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var invoice in invoices)
            {
                var identifier = invoice.Irn?.Value ?? invoice.Id.ToString();

                invoice.SetFIRSSubmissionResponseMessage(message);
                invoice.UpdateStatus(InvoiceStatus.TRANSMISSIONFAILED);
                _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.TRANSMISSIONFAILED, message));

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
