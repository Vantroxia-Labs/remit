using AegisEInvoicing.Application.Common.Extensions;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.FIRSAccessPoint;
using AegisEInvoicing.FIRSAccessPoint.Interfaces;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.ValidateInvoiceData;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateBulkInvoice;

/// <summary>
/// Helper class to track validation statistics across batches
/// </summary>
internal class ValidationStatistics
{
    public int TotalObjects { get; set; }
    public int SuccessfulValidations { get; set; }
    public int FailedValidations { get; set; }
    public Dictionary<string, string> FailedValidationDetails { get; set; } = new();

    public void RecordSuccess()
    {
        SuccessfulValidations++;
    }

    public void RecordFailure(string invoiceIdentifier, string reason)
    {
        FailedValidations++;
        FailedValidationDetails[invoiceIdentifier] = reason;
    }
}

public class ValidateBulkInvoiceCommandHandler(
 IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEncryptionService encryptionService,
    IFIRSHttpClient firsHttpClient,
    ILogger<ValidateBulkInvoiceCommandHandler> logger)
    : IRequestHandler<ValidateBulkInvoiceCommand, ValidateBulkInvoiceResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<ValidateBulkInvoiceCommandHandler> _logger = logger;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly IFIRSHttpClient _firshttpClient = firsHttpClient;

    public async Task<ValidateBulkInvoiceResult> Handle(ValidateBulkInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (!IsUserAuthorized())
            return (ValidateBulkInvoiceResult)ValidateBulkInvoiceResult.AuthorizationError();

        var businessId = _currentUser.BusinessId!.Value;

        // Fetch all valid invoices with their business data in one query
        var invoices = await _context.Invoices
            .Include(i => i.Business)
            .Include(i => i.Party)
            .Include(i => i.InvoiceLine)
                .ThenInclude(il => il.BusinessItem)
            .Include(i => i.BillingReferences)
             .Include(i => i.AdditionalDocumentReferences)
               .Include(i => i.DispatchDocumentReference)
               .Include(i => i.ReceiptDocumentReference)
               .Include(i => i.OriginatorDocumentReference)
               .Include(i => i.ContractDocumentReference)
            .Where(i => request.InvoiceIds.Contains(i.Id)
                     && (i.InvoiceStatus == InvoiceStatus.APPROVED || i.InvoiceStatus == InvoiceStatus.VALIDATIONFAILED)
                     && i.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        if (!invoices.Any())
            return (ValidateBulkInvoiceResult)ValidateBulkInvoiceResult.NotFound("Invoice(s) have either been validated or do not exist");

        // Initialize validation statistics
        var stats = new ValidationStatistics
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
        return new ValidateBulkInvoiceResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = $"Validation completed. {stats.SuccessfulValidations} successful, {stats.FailedValidations} failed out of {stats.TotalObjects} invoices",
            TotalObjects = stats.TotalObjects,
            SuccessfullyValidated = stats.SuccessfulValidations,
            FailedValidation = stats.FailedValidations,
            FailedValidationDetails = stats.FailedValidationDetails
        };
    }

    private async Task ProcessBusinessInvoices(Business business, List<Invoice> invoices, ValidationStatistics stats, CancellationToken cancellationToken)
    {
        // Check if business has FIRS credentials
        if (string.IsNullOrEmpty(business.FIRSApiKey) || string.IsNullOrEmpty(business.FIRSClientSecret))
        {
            await MarkInvoicesAsFailed(invoices, "VALIDATION FAILED: FIRS credentials not configured", stats, cancellationToken);
            return;
        }

        try
        {
            // Decrypt credentials for this specific business
            var (apiKey, clientSecret) = await DecryptCredentials(business.FIRSApiKey, business.FIRSClientSecret);

            // Process invoices in batches for this business
            var batchSize = 10;
            var batches = invoices.Chunk(batchSize);

            foreach (var batch in batches)
            {
                await ProcessInvoiceBatch(batch, business, apiKey, clientSecret, stats, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt credentials for business {BusinessId}", business.Id);
            await MarkInvoicesAsFailed(invoices, "VALIDATION FAILED: Unable to decrypt FIRS credentials", stats, cancellationToken);
        }
    }

    private async Task<(string apiKey, string clientSecret)> DecryptCredentials(string encryptedApiKey, string encryptedClientSecret)
    {
        var decryptTasks = new[]
        {
        _encryptionService.DecryptAsync(encryptedApiKey),
        _encryptionService.DecryptAsync(encryptedClientSecret)
    };

        var results = await Task.WhenAll(decryptTasks);
        return (results[0], results[1]);
    }

    private async Task ProcessInvoiceBatch(IEnumerable<Invoice> invoices, Business business, string apiKey, string clientSecret, ValidationStatistics stats, CancellationToken cancellationToken)
    {
        try
        {
            var validationTasks = invoices.Select(invoice =>
                ValidateSingleInvoice(invoice, business.FIRSBusinessId, apiKey, clientSecret, stats, cancellationToken));

            await Task.WhenAll(validationTasks);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var invoiceIds = string.Join(",", invoices.Select(i => i.Id));
            _logger.LogError(ex, "Failed to validate invoice batch for business {BusinessId}: {InvoiceIds}", business.Id, invoiceIds);

            // Mark failed invoices and record statistics
            foreach (var invoice in invoices)
            {
                var identifier = invoice.Irn?.Value ?? invoice.Id.ToString();
                stats.RecordFailure(identifier, ResponseMessages.INVOICE_VALIDATION_FAILED);

                invoice.SetFIRSSubmissionResponseMessage(ResponseMessages.INVOICE_VALIDATION_FAILED);
                invoice.UpdateStatus(InvoiceStatus.VALIDATIONFAILED);
                _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.VALIDATIONFAILED, ResponseMessages.INVOICE_VALIDATION_FAILED));
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ValidateSingleInvoice(Invoice invoice, Guid firsBusinessId, string apiKey, string clientSecret, ValidationStatistics stats, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating invoice {InvoiceId} with IRN {IRN} for business {BusinessId}",
            invoice.Id, invoice.Irn?.Value, invoice.BusinessId);

        var validationRequest = BuildValidationRequest(invoice, firsBusinessId);
        var response = await _firshttpClient.ValidateInvoiceDataAsync(validationRequest, apiKey, clientSecret, cancellationToken);

        var identifier = invoice.Irn?.Value ?? invoice.Id.ToString();

        if (IsValidationSuccessful(response))
        {
            invoice.SetFIRSSubmissionResponseMessage(ResponseMessages.INVOICE_VALIDATION_SUCCESSFUL);
            invoice.UpdateStatus(InvoiceStatus.VALIDATED);
            _logger.LogInformation("Invoice {InvoiceId} successfully validated by FIRS", invoice.Id);
            _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus,
                ResponseMessages.INVOICE_VALIDATION_SUCCESSFUL));

            // Record success
            stats.RecordSuccess();
        }
        else
        {
            var errorMessage = response?.Error?.PublicMessage ?? response?.Error?.Details ?? ResponseMessages.INVOICE_VALIDATION_FAILED;
            _logger.LogWarning("Invoice {InvoiceId} validation failed with code: {Code}", invoice.Id, response?.Code ?? 0);
            invoice.SetFIRSSubmissionResponseMessage(errorMessage);
            invoice.UpdateStatus(InvoiceStatus.VALIDATIONFAILED);
            _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus, errorMessage));

            // Record failure with details
            stats.RecordFailure(identifier, errorMessage);
        }
    }

    private static ValidateInvoiceDataRequest BuildValidationRequest(Invoice invoice, Guid firsBusinessId) =>
        new()
        {
            BusinessId = firsBusinessId.ToString(),
            Irn = invoice.Irn?.Value ?? string.Empty,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
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
            TaxTotal = invoice.InvoiceLine.ToList().ToTaxTotal(),
            LegalMonetaryTotal = invoice.InvoiceLine.ToList().ToLegalMonetaryTotal(),
            InvoiceLine = invoice.InvoiceLine.ToList().ToInvoiceLine(invoice.Currency.Code)
        };

    private static bool IsValidationSuccessful(dynamic response) =>
        response.Code == HttpStatusCodes.OK.ToInt() || response.Code == 0 || (response.Data?.Ok ?? false);

    private async Task MarkInvoicesAsFailed(IEnumerable<Invoice> invoices, string message, ValidationStatistics stats, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var invoice in invoices)
            {
                var identifier = invoice.Irn?.Value ?? invoice.Id.ToString();

                invoice.SetFIRSSubmissionResponseMessage(message);
                invoice.UpdateStatus(InvoiceStatus.VALIDATIONFAILED);
                _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, InvoiceStatus.VALIDATIONFAILED, message));

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
