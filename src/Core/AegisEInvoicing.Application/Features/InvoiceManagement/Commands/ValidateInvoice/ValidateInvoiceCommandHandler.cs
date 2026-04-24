using AegisEInvoicing.Application.Common.Extensions;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.FIRSAccessPoint;
using AegisEInvoicing.FIRSAccessPoint.Interfaces;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.ValidateInvoiceData;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;

public class ValidateInvoiceCommandHandler(
     IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEncryptionService encryptionService,
    IFIRSHttpClient firsHttpClient,
    ILogger<ValidateInvoiceCommandHandler> logger,
    IInvoiceAuditService? auditService = null,
    ITelemetryService? telemetryService = null)
    : IRequestHandler<ValidateInvoiceCommand, ValidateInvoiceResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<ValidateInvoiceCommandHandler> _logger = logger;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly IFIRSHttpClient _firshttpClient = firsHttpClient;
    private readonly IInvoiceAuditService? _auditService = auditService;
    private readonly ITelemetryService? _telemetryService = telemetryService;

    public async Task<ValidateInvoiceResult> Handle(ValidateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var businessId = ResolveBusinessId(request.BusinessId);
            if (!businessId.HasValue)
                return (ValidateInvoiceResult)ValidateInvoiceResult.AuthorizationError();

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId.Value, cancellationToken);

            if (business is null)
                return ValidateInvoiceResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            var invoice = await _context.Invoices
                .Include(i => i.Business)
                .Include(i => i.Party)
                .Include(i => i.InvoiceLine)
                .ThenInclude(il => il.BusinessItem)
                .Include(i => i.InvoiceApprovalHistory)
                .Include(i => i.BillingReferences)
                 .Include(i => i.AdditionalDocumentReferences)
               .Include(i => i.DispatchDocumentReference)
               .Include(i => i.ReceiptDocumentReference)
               .Include(i => i.OriginatorDocumentReference)
               .Include(i => i.ContractDocumentReference)
                .Where(i => i.Id == request.InvoiceId
                        && (i.InvoiceStatus == InvoiceStatus.APPROVED || i.InvoiceStatus == InvoiceStatus.VALIDATIONFAILED)
                            && i.BusinessId == businessId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (invoice is null)
                return ValidateInvoiceResult.NotFound(ResponseMessages.INVOICE_NOT_FOUND_VALIDATED);

            // Define sets for clarity
            var validStatuses = new[] { InvoiceStatus.APPROVED, InvoiceStatus.VALIDATIONFAILED };
            var validatedStatuses = new[]
            {
                InvoiceStatus.VALIDATED,
                InvoiceStatus.SIGNED,
                InvoiceStatus.SIGNINGFAILED,
                InvoiceStatus.TRANSMITTED,
                InvoiceStatus.TRANSMISSIONFAILED
            };

            if (string.IsNullOrEmpty(invoice.Business.FIRSApiKey) || string.IsNullOrEmpty(invoice.Business.FIRSClientSecret))
                return ValidateInvoiceResult.NotFound(ResponseMessages.BUSINESS_FIRS_CREDENTIALS_NOT_CONFIGURED);

            var decryptedApiKey = await _encryptionService.DecryptAsync(invoice.Business.FIRSApiKey);
            var decryptedClientSecret = await _encryptionService.DecryptAsync(invoice.Business.FIRSClientSecret);

            var validationRequest = new ValidateInvoiceDataRequest
            {
                BusinessId = invoice.Business.FIRSBusinessId.ToString(),
                Irn = invoice.Irn?.Value ?? string.Empty,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                InvoiceKind = invoice.InvoiceKind?.GetDisplayName(),
                IssueTime = invoice.IssueTime,
                InvoiceTypeCode = invoice.InvoiceType.Code.ToString(),
                PaymentStatus = invoice.PaymentStatus.GetDisplayName(),
                Note = invoice.Note,
                DocumentCurrencyCode = invoice.Currency.Code,
                TaxCurrencyCode = invoice.Currency.Code,
                InvoiceDeliveryPeriod = invoice.DeliveryPeriod.ToInvoiceDeliveryPeriod(),
                AccountingCustomerParty = invoice.Party.ToAccountingCustomerParty(),
                AccountingSupplierParty = invoice.Business.ToAccountingSupplierParty(),
                BillingReference = invoice.BillingReferences.ToList().ToBillingReference(),
                DocumentReference = invoice.AdditionalDocumentReferences.ToList().ToAddtionalDocumentReference(),
                DispatchDocumentReference = invoice.DispatchDocumentReference!.ToDispatchDocumentReference(),
                ReceiptDocumentReference = invoice.ReceiptDocumentReference!.ToReceiptDocumentReference(),
                OriginatorDocumentReference = invoice.OriginatorDocumentReference!.ToOriginatorDocumentReference(),
                ContractDocumentReference = invoice.ContractDocumentReference!.ToContractDocumentReference(),
                PaymentMeans = invoice.PaymentMeans!.ToPaymentMeans(invoice.IssueDate.AddDays(7)),
                PaymentTermsNote = invoice.PaymentTerms,
                AllowanceCharge = invoice.InvoiceLine.ToList().ToAllowanceCharge(),
                LegalMonetaryTotal = invoice.InvoiceLine.ToList().ToLegalMonetaryTotal(),
                TaxTotal = invoice.InvoiceLine.ToList().ToTaxTotal(),
                InvoiceLine = invoice.InvoiceLine.ToList().ToInvoiceLine(invoice.Currency.Code)
            };

            var statusBefore = invoice.InvoiceStatus.ToString();

            var apiCallStart = DateTime.UtcNow;
            var response = await _firshttpClient.ValidateInvoiceDataAsync(validationRequest, decryptedApiKey, decryptedClientSecret, cancellationToken);
            var apiCallDuration = DateTime.UtcNow - apiCallStart;

            // Track FIRS API dependency
            _telemetryService?.TrackDependency(
                "HTTP",
                "FIRS",
                "ValidateInvoice",
                apiCallDuration,
                response?.Code == HttpStatusCodes.OK.ToInt() || response?.Code == 0,
                response?.Code,
                response?.Code == HttpStatusCodes.OK.ToInt() || response?.Code == 0 ? null : response?.Error?.PublicMessage);

            // Log external service interaction
            if (_auditService != null)
            {
                await _auditService.LogExternalServiceInteractionAsync(
                    invoice.Id,
                    "FIRS",
                    "ValidateInvoice",
                    $"IRN: {invoice.Irn?.Value}",
                    System.Text.Json.JsonSerializer.Serialize(response),
                    response?.Code == HttpStatusCodes.OK.ToInt() || response?.Code == 0,
                    cancellationToken: cancellationToken);
            }

            if (response?.Code == HttpStatusCodes.OK.ToInt() || response?.Code == 0 || (response?.Data?.Ok ?? false))
            {
                invoice.SetFIRSSubmissionResponseMessage("Invoice Validated");
                invoice.UpdateStatus(InvoiceStatus.VALIDATED);
                var invoiceApprovalHistory = InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus, ResponseMessages.INVOICE_VALIDATION_SUCCESSFUL);
                _context.InvoiceApprovalHistories.Add(invoiceApprovalHistory);
                if (invoice.InvoiceSource == InvoiceSource.SFTP)
                {
                    User? currentUser = null;
                    currentUser = await _context.Users
                        .FirstOrDefaultAsync(u =>
                                                 u.BusinessId == request.BusinessId &&
                                                 !u.IsDeleted, cancellationToken);

                    var createdById = currentUser?.Id ?? Guid.Empty;
                    invoiceApprovalHistory.CreatedBy = createdById;
                }
                await _context.SaveChangesAsync(cancellationToken);

                // Log state transition
                if (_auditService != null)
                {
                    await _auditService.LogStateTransitionAsync(
                        invoice.Id,
                        "ValidateInvoice",
                        statusBefore,
                        InvoiceStatus.VALIDATED.ToString(),
                        _currentUser.UserId,
                        null, // IP address - can be injected via HttpContext if needed
                        cancellationToken: cancellationToken);
                }

                _logger.LogInformation("Invoice {InvoiceId} successfully validated by FIRS", invoice.Id);

                // Track successful validation
                var duration = DateTime.UtcNow - startTime;
                _telemetryService?.TrackInvoiceValidated(invoice.Id, true, duration);
            }
            else
            {
                _logger.LogWarning("Invoice {InvoiceId} validation failed with code: {Code}",
                    invoice.Id, response?.Code);

                invoice.SetFIRSSubmissionResponseMessage(response?.Error?.PublicMessage);
                invoice.UpdateStatus(InvoiceStatus.VALIDATIONFAILED);
                var invoiceApprovalHistory = InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus, response?.Error?.PublicMessage ?? response?.Error?.Details ?? ResponseMessages.INVOICE_VALIDATION_FAILED);
                _context.InvoiceApprovalHistories.Add(invoiceApprovalHistory);
                if (invoice.InvoiceSource == InvoiceSource.SFTP)
                {
                    User? currentUser = null;
                    currentUser = await _context.Users
                        .FirstOrDefaultAsync(u =>
                                                 u.BusinessId == request.BusinessId &&
                                                 !u.IsDeleted, cancellationToken);

                    var createdById = currentUser?.Id ?? Guid.Empty;
                    invoiceApprovalHistory.CreatedBy = createdById;
                }
                await _context.SaveChangesAsync(cancellationToken);

                // Log failed state transition
                if (_auditService != null)
                {
                    await _auditService.LogStateTransitionAsync(
                        invoice.Id,
                        "ValidateInvoice",
                        statusBefore,
                        InvoiceStatus.VALIDATIONFAILED.ToString(),
                        _currentUser.UserId,
                        null,
                        System.Text.Json.JsonSerializer.Serialize(new { ErrorCode = response?.Code, ErrorMessage = response?.Error?.PublicMessage }),
                        cancellationToken);
                }

                // Track failed validation
                var duration = DateTime.UtcNow - startTime;
                var errorMessage = response?.Error?.PublicMessage ?? response?.Error?.Details ?? "Validation failed";
                _telemetryService?.TrackInvoiceValidated(invoice.Id, false, duration, errorMessage);

                return new ValidateInvoiceResult
                {
                    IsSuccess = false,
                    StatusCodes = response?.Code ?? 500,
                    Message = response?.Error switch
                    {
                        { Details: { } details } when !string.IsNullOrWhiteSpace(details)
                            => $"FIRS Response: {details}",

                        { PublicMessage: { } publicMsg } when !string.IsNullOrWhiteSpace(publicMsg)
                            => $"FIRS Response: {publicMsg}",

                        _ when !string.IsNullOrWhiteSpace(response?.Message)
                            => $"FIRS Response: {response!.Message}",

                        _ => $"FIRS Response: {response!.Error!.Details}"
                    }
                };
            }

            return ValidateInvoiceResult.Successful();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;

            _logger.LogError(ex, "Failed to validate invoice {InvoiceId}", request.InvoiceId);

            // Track failed validation due to exception
            _telemetryService?.TrackInvoiceValidated(request.InvoiceId, false, duration, ex.Message);

            return ValidateInvoiceResult.Failure(ResponseMessages.INVOICE_VALIDATION_FAILED);
        }
    }

    private Guid? ResolveBusinessId(Guid? requestBusinessId)
    {
        // Normal HTTP requests: BusinessId must come from claims.
        if (_currentUser.BusinessId.HasValue)
        {
            // If caller also provided a businessId, enforce it matches the authenticated context.
            if (requestBusinessId.HasValue && requestBusinessId.Value != _currentUser.BusinessId.Value)
                return null;

            return _currentUser.BusinessId;
        }

        // Background jobs / non-HTTP callers: allow explicit businessId.
        return requestBusinessId;
    }
}
