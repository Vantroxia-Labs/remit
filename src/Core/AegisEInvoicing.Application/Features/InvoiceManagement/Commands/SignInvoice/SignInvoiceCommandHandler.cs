using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignInvoice;

public class SignInvoiceCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IAppProviderRouter appProviderRouter,
    ILogger<SignInvoiceCommandHandler> logger,
    IInvoiceAuditService? auditService = null,
    ITelemetryService? telemetryService = null)
    : IRequestHandler<SignInvoiceCommand, SignInvoiceResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<SignInvoiceCommandHandler> _logger = logger;
    private readonly IAppProviderRouter _appProviderRouter = appProviderRouter;
    private readonly IInvoiceAuditService? _auditService = auditService;
    private readonly ITelemetryService? _telemetryService = telemetryService;

    public async Task<SignInvoiceResult> Handle(SignInvoiceCommand request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var businessId = ResolveBusinessId(request.BusinessId);
            if (!businessId.HasValue)
                return SignInvoiceResult.AuthorizationError();

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId.Value, cancellationToken);
            if (business is null)
                return SignInvoiceResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

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
               && (i.InvoiceStatus == InvoiceStatus.VALIDATED || i.InvoiceStatus == InvoiceStatus.SIGNINGFAILED)
                           && i.BusinessId == businessId.Value)
               .FirstOrDefaultAsync(cancellationToken);

            if (invoice is null)
                return SignInvoiceResult.NotFound(ResponseMessages.INVOICE_NOT_FOUND_SIGNED);

            // Define sets for clarity
            var validStatuses = new[] { InvoiceStatus.VALIDATED, InvoiceStatus.SIGNINGFAILED };
            var validatedStatuses = new[]
            {
                InvoiceStatus.SIGNED,
                InvoiceStatus.TRANSMITTED,
                InvoiceStatus.TRANSMISSIONFAILED
            };

            // Check historical status
            //if (invoice.InvoiceApprovalHistory.Any(h => validatedStatuses.Contains(h.InvoiceStatus)))
            //    return SignInvoiceResult.BadRequest(ResponseMessages.INVOICE_ALREADY_SIGNED);

            // Get the configured provider adapter for this business
            var provider = await _appProviderRouter.GetProviderAsync(businessId.Value, cancellationToken);
            var providerName = provider.DisplayName;

            var statusBefore = invoice.InvoiceStatus.ToString();

            var apiCallStart = DateTime.UtcNow;
            var result = await provider.SignInvoiceAsync(invoice, cancellationToken);
            var apiCallDuration = DateTime.UtcNow - apiCallStart;

            // Track APP provider API dependency
            _telemetryService?.TrackDependency(
                "HTTP",
                providerName,
                "SignInvoice",
                apiCallDuration,
                result.IsSuccess,
                null, // ErrorCode is string, not compatible with int? status code
                result.IsSuccess ? null : result.ErrorMessage);

            // Log external service interaction
            if (_auditService != null)
            {
                await _auditService.LogExternalServiceInteractionAsync(
                    invoice.Id,
                    providerName,
                    "SignInvoice",
                    $"IRN: {invoice.Irn?.Value}",
                    result.RawResponse ?? System.Text.Json.JsonSerializer.Serialize(result),
                    result.IsSuccess,
                    cancellationToken: cancellationToken);
            }

            if (result.IsSuccess)
            {
                invoice.SetFIRSSubmissionResponseMessage(ResponseMessages.INVOICE_SIGNING_SUCCESSFUL);
                invoice.UpdateStatus(InvoiceStatus.SIGNED);
                var invoiceApprovalHistory = InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus, ResponseMessages.INVOICE_SIGNING_SUCCESSFUL);
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
                        "SignInvoice",
                        statusBefore,
                        InvoiceStatus.SIGNED.ToString(),
                        _currentUser.UserId,
                        null,
                        cancellationToken: cancellationToken);
                }

                _logger.LogInformation("Invoice {InvoiceId} successfully Signed by FIRS", invoice.Id);

                // Track successful signing
                var duration = DateTime.UtcNow - startTime;
                _telemetryService?.TrackInvoiceSigned(invoice.Id, true, duration);
            }
            else
            {
                _logger.LogWarning("Invoice {InvoiceId} Signing failed with error: {ErrorCode} - {ErrorMessage}",
                    invoice.Id, result.ErrorCode, result.ErrorMessage);

                // Extract error message from result
                var errorMessage = result.ErrorMessage ?? "Invoice signing failed";

                invoice.SetFIRSSubmissionResponseMessage(errorMessage);
                invoice.UpdateStatus(InvoiceStatus.SIGNINGFAILED);
                var invoiceApprovalHistory = InvoiceApprovalHistory.Create(
                    invoice.Id,
                    InvoiceStatus.SIGNINGFAILED,
                    errorMessage);
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
                        "SignInvoice",
                        statusBefore,
                        InvoiceStatus.SIGNINGFAILED.ToString(),
                        _currentUser.UserId,
                        null,
                        System.Text.Json.JsonSerializer.Serialize(new
                        {
                            ErrorCode = result.ErrorCode,
                            ErrorMessage = errorMessage,
                            Provider = providerName
                        }),
                        cancellationToken);
                }

                // Track failed signing
                var duration = DateTime.UtcNow - startTime;
                _telemetryService?.TrackInvoiceSigned(invoice.Id, false, duration, errorMessage);

                return new SignInvoiceResult
                {
                    IsSuccess = false,
                    StatusCodes = 400,
                    Message = errorMessage
                };
            }
            return SignInvoiceResult.Successful();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;

            _logger.LogError(ex, "Failed to Sign Invoice {InvoiceId}: {ExceptionMessage}",
                request.InvoiceId, ex.Message);

            // Try to extract Interswitch error details from exception
            var errorMessage = ExtractErrorMessageFromException(ex);

            // Track failed signing due to exception
            _telemetryService?.TrackInvoiceSigned(request.InvoiceId, false, duration, errorMessage);

            return SignInvoiceResult.Failure(errorMessage);
        }
    }

    /// <summary>
    /// Extracts user-friendly error message from exception
    /// </summary>
    private string ExtractErrorMessageFromException(Exception ex)
    {
        // Check if exception message contains JSON response (common with HttpClient errors)
        var exceptionMessage = ex.Message;

        // Try to find "public_message" in the exception or inner exceptions
        if (exceptionMessage.Contains("public_message", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // Extract JSON from exception message if possible
                var jsonStart = exceptionMessage.IndexOf('{');
                if (jsonStart >= 0)
                {
                    var jsonEnd = exceptionMessage.LastIndexOf('}');
                    if (jsonEnd > jsonStart)
                    {
                        var jsonContent = exceptionMessage.Substring(jsonStart, jsonEnd - jsonStart + 1);
                        using var doc = System.Text.Json.JsonDocument.Parse(jsonContent);

                        // Try to get error.public_message
                        if (doc.RootElement.TryGetProperty("error", out var errorElement))
                        {
                            if (errorElement.TryGetProperty("public_message", out var publicMessage))
                            {
                                return publicMessage.GetString() ?? ResponseMessages.INVOICE_SIGNING_FAILED;
                            }
                            if (errorElement.TryGetProperty("details", out var details))
                            {
                                return details.GetString() ?? ResponseMessages.INVOICE_SIGNING_FAILED;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore JSON parsing errors
            }
        }

        // Check inner exception
        if (ex.InnerException != null)
        {
            var innerMessage = ExtractErrorMessageFromException(ex.InnerException);
            if (innerMessage != ResponseMessages.INVOICE_SIGNING_FAILED)
                return innerMessage;
        }

        // Return exception message if it's meaningful (not too technical)
        if (!string.IsNullOrWhiteSpace(exceptionMessage) &&
            exceptionMessage.Length < 200 &&
            !exceptionMessage.Contains("Exception", StringComparison.OrdinalIgnoreCase))
        {
            return exceptionMessage;
        }

        // Default fallback
        return ResponseMessages.INVOICE_SIGNING_FAILED;
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
}
