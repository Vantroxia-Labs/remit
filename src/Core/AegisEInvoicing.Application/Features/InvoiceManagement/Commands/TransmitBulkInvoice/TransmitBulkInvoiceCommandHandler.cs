using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.ValueObjects;
using AegisEInvoicing.Interswitch.Interfaces;
using AegisEInvoicing.Interswitch.Models;
using AegisEInvoicing.Interswitch.Models.Requests.LookupWithTIN;
using AegisEInvoicing.Interswitch.Models.Requests.TransmitInvoice;
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
    IEncryptionService encryptionService,
    IInterswitchHttpClient interswitchHttpClient,
    ILogger<TransmitBulkInvoiceCommandHandler> logger)
    : IRequestHandler<TransmitBulkInvoiceCommand, TransmitBulkInvoiceResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<TransmitBulkInvoiceCommandHandler> _logger = logger;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly IInterswitchHttpClient _interswitchHttpClient = interswitchHttpClient;

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
                .ThenInclude(bi => bi.ItemCategory)
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
            // Process invoices in batches for this business
            var batchSize = 10;
            var batches = invoices.Chunk(batchSize);

            foreach (var batch in batches)
            {
                await ProcessInvoiceBatch(batch, business, stats, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process business invoices for business {BusinessId}", business.Id);
            await MarkInvoicesAsFailed(invoices, "TRANSMISSION FAILED: Unable to process invoices", stats, cancellationToken);
        }
    }

    private async Task ProcessInvoiceBatch(IEnumerable<Invoice> invoices, Business business, TransmissionStatistics stats, CancellationToken cancellationToken)
    {
        try
        {
            var transmissionTasks = invoices.Select(invoice =>
                TransmitSingleInvoice(invoice, stats, cancellationToken));

            await Task.WhenAll(transmissionTasks);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var invoiceIds = string.Join(",", invoices.Select(i => i.Id));
            _logger.LogError(ex, "Failed to transmit invoice batch for business {BusinessId}: {InvoiceIds}", business.Id, invoiceIds);

            // Mark failed invoices and record statistics
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

    private async Task TransmitSingleInvoice(Invoice invoice, TransmissionStatistics stats, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Transmitting invoice {InvoiceId} with IRN {IRN} for business {BusinessId}",
            invoice.Id, invoice.Irn?.Value, invoice.BusinessId);

        var lookupRequest = BuildLookUpWithTinRequest(invoice.Party.TaxIdentificationNumber);
        var isTinValid = await _interswitchHttpClient.LookupWithTINAsync(lookupRequest, cancellationToken);

        var identifier = invoice.Irn?.Value ?? invoice.Id.ToString();

        if (!isTinValid.Data!.Data!.IsUp)
        {
            var errorMessage = ResponseMessages.PARTY_NOT_FOUND;
            _logger.LogWarning("Invoice {InvoiceId} transmission failed with code: {Code}, message: {Message}",
                invoice.Id, 400, errorMessage);
            invoice.SetFIRSSubmissionResponseMessage(errorMessage);
            invoice.UpdateStatus(InvoiceStatus.TRANSMISSIONFAILED);
            _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.TRANSMISSIONFAILED, errorMessage));

            // Record failure with details
            stats.RecordFailure(identifier, errorMessage);
        }
        else
        {
            var transmissionRequest = BuildTransmissionRequest(invoice.Irn!);
            var response = await _interswitchHttpClient.TransmitInvoiceAsync(transmissionRequest, cancellationToken);

            if (response.Data!.Data!.Ok)
            {
                invoice.SetFIRSSubmissionResponseMessage(ResponseMessages.INVOICE_TRANSMISSION_SUCCESSFUL);
                invoice.UpdateStatus(InvoiceStatus.TRANSMITTED);
                _logger.LogInformation("Invoice {InvoiceId} successfully transmitted to Party", invoice.Id);
                _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus,
                    ResponseMessages.INVOICE_TRANSMISSION_SUCCESSFUL));

                // Record success
                stats.RecordSuccess();
            }
            else
            {
                var errorMessage = response?.Data?.Error?.PublicMessage ?? response?.Data?.Error?.Details ?? ResponseMessages.INVOICE_TRANSMISSION_FAILED;
                _logger.LogWarning("Invoice {InvoiceId} transmission failed with code: {Code}, message: {Message}",
                    invoice.Id, response?.Data?.Code, errorMessage);
                invoice.SetFIRSSubmissionResponseMessage(errorMessage);
                invoice.UpdateStatus(InvoiceStatus.TRANSMISSIONFAILED);
                _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.TRANSMISSIONFAILED, errorMessage));

                // Record failure with details
                stats.RecordFailure(identifier, errorMessage);
            }
        }
    }

    private static TransmitInvoiceRequest BuildTransmissionRequest(IRN irn) =>
        new()
        {
           IRN = irn.Value
        };

    private static LookupWithTINRequest BuildLookUpWithTinRequest(TIN tin) =>
        new()
        {
            TIN = tin.Value
        };

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
