using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.TransmitInvoice;

public class TransmitInvoiceCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IAppProviderRouter appProviderRouter,
    ILogger<TransmitInvoiceCommandHandler> logger,
    ITelemetryService? telemetryService = null)
    : IRequestHandler<TransmitInvoiceCommand, TransmitInvoiceResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<TransmitInvoiceCommandHandler> _logger = logger;
    private readonly IAppProviderRouter _appProviderRouter = appProviderRouter;
    private readonly ITelemetryService? _telemetryService = telemetryService;

    public async Task<TransmitInvoiceResult> Handle(TransmitInvoiceCommand request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var businessId = ResolveBusinessId(request.BusinessId);
            if (!businessId.HasValue)
                return (TransmitInvoiceResult)TransmitInvoiceResult.AuthorizationError();

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId.Value, cancellationToken);
            if (business is null)
                return TransmitInvoiceResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            var invoice = await _context.Invoices
               .Include(i => i.Business)
               .Include(i => i.Party)
               .Include(i => i.InvoiceLine)
               .ThenInclude(il => il.BusinessItem)
               .ThenInclude(il => il!.ItemCategory)
               .Include(i => i.InvoiceApprovalHistory)
               .Where(i => i.Id == request.InvoiceId
                           && i.BusinessId == businessId.Value)
               .FirstOrDefaultAsync(cancellationToken);

            if (invoice is null)
                return TransmitInvoiceResult.NotFound(ResponseMessages.INVOICE_NOT_FOUND_TRANSMITTED);

            if (invoice.InvoiceKind == InvoiceKind.B2C)
                return TransmitInvoiceResult.BadRequest(ResponseMessages.B2C_INVOICE_CANNOT_BE_TRANSMITTED);

            var validStatuses = new[] { InvoiceStatus.SIGNED, InvoiceStatus.TRANSMISSIONFAILED };
            var validatedStatuses = new[]
            {
                InvoiceStatus.TRANSMITTED,
                InvoiceStatus.TRANSMITTING
            };

            (int code, string message) result;

            // Check historical status
            if (invoice.InvoiceApprovalHistory.Any(h
                => h.InvoiceId == invoice.Id
                   && validatedStatuses.Contains(h.InvoiceStatus)))
                return TransmitInvoiceResult.BadRequest(ResponseMessages.INVOICE_ALREADY_TRANSMITTED);

            // Resolve the APP provider configured for this business
            var appProvider = await _appProviderRouter.GetProviderAsync(businessId.Value, cancellationToken);

            //Seun: 20/01/2026 FIRS Relaxed TIN Validation
            //Seun: 26/02/2026 - Unrelaxed this because for Tax parties without valid TIN/have not enrolled on the NRS MBS System transmission fails

            if (appProvider.SupportsLookupTin)
            {
                var tinLookup = await appProvider.LookupTinAsync(invoice.Party.TaxIdentificationNumber.Value, cancellationToken);

                if (!tinLookup.IsSuccess || !tinLookup.IsUp)
                {
                    var errorMessage = ResponseMessages.INVALID_TIN_OR_NOT_ENROLLED;

                    _logger.LogWarning(
                        "Invoice {InvoiceId} transmission failed. TIN {Tin} is invalid or not enrolled on MBS portal",
                        invoice.Id,
                        MaskTin(invoice.Party.TaxIdentificationNumber.Value));

                    invoice.SetFIRSSubmissionResponseMessage(errorMessage);
                    invoice.UpdateStatus(InvoiceStatus.TRANSMISSIONFAILED);
                    var approvalHistory = InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.TRANSMISSIONFAILED, errorMessage);
                    _context.InvoiceApprovalHistories.Add(approvalHistory);

                    if (invoice.InvoiceSource == InvoiceSource.SFTP)
                    {
                        var sftpUser = await _context.Users
                            .FirstOrDefaultAsync(u => u.BusinessId == request.BusinessId && !u.IsDeleted, cancellationToken);
                        approvalHistory.CreatedBy = sftpUser?.Id ?? Guid.Empty;
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    return TransmitInvoiceResult.BadRequest(errorMessage);
                }

                _logger.LogInformation(
                    "Invoice {InvoiceId}: TIN validation successful. Business: {BusinessRef}",
                    invoice.Id,
                    tinLookup.BusinessReference ?? "N/A");
            }

            var apiCallStart = DateTime.UtcNow;
            var transmitResult = await appProvider.TransmitAsync(invoice.Irn.Value, cancellationToken);
            var apiCallDuration = DateTime.UtcNow - apiCallStart;

            _telemetryService?.TrackDependency(
                "HTTP",
                appProvider.ProviderCode,
                "TransmitInvoice",
                apiCallDuration,
                transmitResult.IsSuccess,
                transmitResult.IsSuccess ? 200 : 0,
                transmitResult.ErrorMessage);

            if (transmitResult.IsSuccess)
            {
                invoice.SetFIRSSubmissionResponseMessage(ResponseMessages.INVOICE_TRANSMISSION_SUCCESSFUL);
                invoice.UpdateStatus(InvoiceStatus.TRANSMITTED);
                _logger.LogInformation("Invoice {InvoiceId} successfully transmitted via {Provider}", invoice.Id, appProvider.ProviderCode);

                var duration = DateTime.UtcNow - startTime;
                _telemetryService?.TrackInvoiceTransmitted(invoice.Id, true, duration);

                var invoiceApprovalHistory = InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus, ResponseMessages.INVOICE_TRANSMISSION_SUCCESSFUL);
                _context.InvoiceApprovalHistories.Add(invoiceApprovalHistory);
                result = (200, invoice.FIRSSubmissionResponseMessage!);

                if (invoice.InvoiceSource == InvoiceSource.SFTP)
                {
                    var sftpUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.BusinessId == request.BusinessId && !u.IsDeleted, cancellationToken);
                    invoiceApprovalHistory.CreatedBy = sftpUser?.Id ?? Guid.Empty;
                }
            }
            else
            {
                var errorMessage = transmitResult.ErrorMessage ?? "Transmission failed";

                _logger.LogWarning("Invoice {InvoiceId} transmission failed via {Provider}: {Message}",
                    invoice.Id, appProvider.ProviderCode, errorMessage);

                var duration = DateTime.UtcNow - startTime;
                _telemetryService?.TrackInvoiceTransmitted(invoice.Id, false, duration, errorMessage);

                invoice.SetFIRSSubmissionResponseMessage(errorMessage);
                invoice.UpdateStatus(InvoiceStatus.TRANSMISSIONFAILED);
                var invoiceApprovalHistory = InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus, errorMessage);
                _context.InvoiceApprovalHistories.Add(invoiceApprovalHistory);

                if (invoice.InvoiceSource == InvoiceSource.SFTP)
                {
                    var sftpUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.BusinessId == request.BusinessId && !u.IsDeleted, cancellationToken);
                    invoiceApprovalHistory.CreatedBy = sftpUser?.Id ?? Guid.Empty;
                }

                result = (0, invoice.FIRSSubmissionResponseMessage!);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return result.code switch
            {
                200 => TransmitInvoiceResult.Successful(),
                404 => TransmitInvoiceResult.NotFound(result.message),
                _ => TransmitInvoiceResult.BadRequest(result.message)
            };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;

            _logger.LogError(ex, "Failed to transmit invoice {InvoiceId}", request.InvoiceId);

            // Track failed transmission due to exception
            _telemetryService?.TrackInvoiceTransmitted(request.InvoiceId, false, duration, ex.Message);

            return TransmitInvoiceResult.Failure(ResponseMessages.INVOICE_TRANSMISSION_FAILED);
        }
    }

    private Guid? ResolveBusinessId(Guid? requestBusinessId)
    {
        if (_currentUser.BusinessId.HasValue)
        {
            if (requestBusinessId.HasValue && requestBusinessId.Value != _currentUser.BusinessId.Value)
                return null;

            return _currentUser.BusinessId;
        }

        return requestBusinessId;
    }

    /// <summary>
    /// Mask TIN for logging (show only last 4 digits)
    /// </summary>
    private static string MaskTin(string tin)
    {
        if (string.IsNullOrWhiteSpace(tin) || tin.Length < 4)
            return "****";

        return $"***********{tin[^4..]}";
    }
}
