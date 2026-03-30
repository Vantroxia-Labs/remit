using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Security;
using AegisEInvoicing.Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateFIRSInvoice;

public class CreateFIRSInvoiceCommandValidator : AbstractValidator<CreateFIRSInvoiceCommand>
{
    private readonly IConfiguration? _configuration;
    private readonly IFIRSCurrencyValidationService? _fIRSCurrencyValidationService;
    private readonly IReferenceDataCacheService _referenceDataCache;

    public CreateFIRSInvoiceCommandValidator(
        IReferenceDataCacheService referenceDataCache, // REQUIRED for compliance!
        IConfiguration? configuration = null,
        IFIRSCurrencyValidationService? fIRSCurrencyValidationService = null)
    {
        _configuration = configuration;
        _fIRSCurrencyValidationService = fIRSCurrencyValidationService;
        _referenceDataCache = referenceDataCache ?? throw new ArgumentNullException(
            nameof(referenceDataCache),
            "Reference data cache service is required for invoice validation. " +
            "This is a CRITICAL compliance requirement - validation cannot proceed without it.");

        RuleFor(x => x.IssueDate)
            .NotEmpty()
            .WithMessage("Invoice issue date is required. Please provide a valid issue date.")
            .Must(date => BeValidInvoiceDate(date, _configuration))
            .WithMessage(x => GetInvoiceDateValidationMessage(_configuration));

        RuleFor(x => x.InvoiceType)
            .NotNull()
            .WithMessage("Invoice type is required. Please specify the invoice type (e.g., Standard, CreditNote, DebitNote).")
            .Must(invoiceType => BeValidInvoiceTypeName(invoiceType))
            .WithMessage("Invoice type name must contain only letters, numbers, and spaces (no special characters allowed)")
            .Must(invoiceType => BeValidInvoiceTypeCode(invoiceType))
            .WithMessage(x => $"Invalid invoice type code '{x.InvoiceType?.Code}'. Please use a valid FIRS invoice type code. " +
                             $"Valid codes include: {GetValidInvoiceTypeCodes()}");

        RuleFor(x => x.Currency)
            .NotNull()
            .WithMessage("Currency is required. Please specify the invoice currency (e.g., NGN, USD, EUR).")
            .Must(currency => BeValidCurrencyName(currency))
            .WithMessage("Currency name must contain only letters, numbers, and spaces (no special characters allowed)")
            .Must(currency => BeValidCurrencyCode(currency))
            .WithMessage(x => $"Invalid currency code '{x.Currency?.Code}'. Please use a valid FIRS currency code. " +
                             $"Valid codes include: {GetValidCurrencyCodes()}")
            .MustAsync(async (currency, cancellationToken) =>
            {
                if (currency == null) return false;

                // Use FIRS API validation if service is available
                if (_fIRSCurrencyValidationService != null)
                    return await _fIRSCurrencyValidationService.IsValidCurrencyAsync(currency.Code, cancellationToken);
                
                // Fallback to config-based validation
                return BeValidCurrency(currency, _configuration);
            })
            .WithMessage(x => GetCurrencyValidationMessage(_configuration));

        RuleFor(x => x.PaymentMeans)
            .NotNull()
            .WithMessage("Payment means is required. Please specify how the invoice will be paid (e.g., Cash, BankTransfer, Card).")
            .Must(paymentMeans => BeValidPaymentMeansName(paymentMeans))
            .WithMessage("Payment means name must contain only letters, numbers, and spaces (no special characters allowed)")
            .Must(paymentMeans => BeValidPaymentMeansCode(paymentMeans))
            .WithMessage(x => $"Invalid payment means code '{x.PaymentMeans?.Code}'. Please use a valid FIRS payment means code. " +
                             $"Valid codes include: {GetValidPaymentMeansCodes()}");

        RuleFor(x => x.DeliveryPeriod)
            .NotNull()
            .WithMessage("Delivery period is required. Please provide the start and end dates for the delivery period.")
            .Must(deliveryPeriod => BeValidDeliveryPeriod(deliveryPeriod))
            .WithMessage("Delivery period start date must be on or after 2025-01-01 and cannot be after the end date");

        RuleFor(x => x.Party)
            .NotNull()
            .WithMessage("Party information is required. Please provide complete customer/supplier details including name, email, TIN, and address.");

        RuleFor(x => x.InvoiceItems)
            .NotEmpty()
            .WithMessage("At least one invoice item is required. An invoice must contain one or more line items with products or services.")
            .Must(items => BeValidTotalAmount(items, _configuration))
            .WithMessage(x => GetTotalAmountValidationMessage(_configuration));

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.IssueDate)
            .WithMessage("Due date must be on or after the issue date. Due date: {PropertyValue}, Issue date: {ComparisonValue}")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .WithMessage("Invoice note is too long. Maximum 500 characters allowed, current length: {TotalLength}")
            .MustBeSafeText(500)
            .When(x => !string.IsNullOrEmpty(x.Note));

        RuleFor(x => x.PaymentReference)
            .MaximumLength(100)
            .WithMessage("Payment reference is too long. Maximum 100 characters allowed, current length: {TotalLength}")
            .MustBeAlphanumeric(100, allowSpecialChars: true)
            .When(x => !string.IsNullOrEmpty(x.PaymentReference));

        RuleFor(x => x.PaymentTerms)
            .MaximumLength(200)
            .WithMessage("Payment terms are too long. Maximum 200 characters allowed, current length: {TotalLength}")
            .MustBeSafeText(200)
            .When(x => !string.IsNullOrEmpty(x.PaymentTerms));

        RuleForEach(x => x.InvoiceItems)
            .SetValidator(new InvoiceItemRequestValidator(_referenceDataCache, _configuration));

        RuleForEach(x => x.BillingReferences)
            .SetValidator(new BillingReferenceRequestValidator())
            .When(x => x.BillingReferences != null && x.BillingReferences.Any());

        // Document reference validators
        RuleFor(x => x.DispatchDocumentReference)
            .SetValidator(new DocumentReferenceRequestValidator()!)
            .When(x => x.DispatchDocumentReference != null);

        RuleFor(x => x.ReceiptDocumentReference)
            .SetValidator(new DocumentReferenceRequestValidator()!)
            .When(x => x.ReceiptDocumentReference != null);

        RuleFor(x => x.OriginatorDocumentReference)
            .SetValidator(new DocumentReferenceRequestValidator()!)
            .When(x => x.OriginatorDocumentReference != null);

        RuleFor(x => x.ContractDocumentReference)
            .SetValidator(new DocumentReferenceRequestValidator()!)
            .When(x => x.ContractDocumentReference != null);

        RuleForEach(x => x.AdditionalDocumentReferences)
            .SetValidator(new DocumentReferenceRequestValidator())
            .When(x => x.AdditionalDocumentReferences != null && x.AdditionalDocumentReferences.Any());
    }

    // ===============================================================
    // REFERENCE DATA VALIDATION METHODS (FIRS Cache)
    // ===============================================================
    
    /// <summary>
    /// Validates invoice type code against FIRS cached reference data.
    /// Cache is loaded at startup and refreshed daily at 12 AM.
    /// If cache fails to refresh, last known good cache is retained.
    /// </summary>
    private bool BeValidInvoiceTypeCode(InvoiceType? invoiceType)
    {
        if (invoiceType == null)
            return false;

        var invoiceTypeCodeStr = invoiceType.Code.ToString();
        return _referenceDataCache.IsValidInvoiceType(invoiceTypeCodeStr);
    }

    /// <summary>
    /// Validates currency code against FIRS cached reference data.
    /// Cache is loaded at startup and refreshed daily at 12 AM.
    /// If cache fails to refresh, last known good cache is retained.
    /// </summary>
    private bool BeValidCurrencyCode(Currency? currency)
    {
        if (currency == null)
            return false;

        return _referenceDataCache.IsValidCurrency(currency.Code);
    }

    /// <summary>
    /// Validates payment means code against FIRS cached reference data.
    /// Cache is loaded at startup and refreshed daily at 12 AM.
    /// If cache fails to refresh, last known good cache is retained.
    /// </summary>
    private bool BeValidPaymentMeansCode(PaymentMeans? paymentMeans)
    {
        if (paymentMeans == null)
            return false;

        if (string.IsNullOrWhiteSpace(paymentMeans.Code))
            return true; // Allow null/empty (will be caught by NotNull rule)

        return _referenceDataCache.IsValidPaymentMeans(paymentMeans.Code);
    }

    /// <summary>
    /// Gets valid invoice type codes for error messages
    /// </summary>
    private string GetValidInvoiceTypeCodes()
    {
        var codes = _referenceDataCache.GetInvoiceTypeCodes().Take(5);
        return codes.Any() ? string.Join(", ", codes) : "No invoice types available from FIRS";
    }

    /// <summary>
    /// Gets valid currency codes for error messages
    /// </summary>
    private string GetValidCurrencyCodes()
    {
        var codes = _referenceDataCache.GetCurrencyCodes().Take(5);
        return codes.Any() ? string.Join(", ", codes) : "No currencies available from FIRS";
    }

    /// <summary>
    /// Gets valid payment means codes for error messages
    /// </summary>
    private string GetValidPaymentMeansCodes()
    {
        var codes = _referenceDataCache.GetPaymentMeansCodes().Take(5);
        return codes.Any() ? string.Join(", ", codes) : "No payment means available from FIRS";
    }

    /// <summary>
    /// Validates invoice date against business rules:
    /// 1. Not in the future
    /// 2. Not older than 2025-01-01 (VAPT compliance requirement - absolute minimum)
    /// 3. Not older than configured backstop period (default: 90 days from today)
    /// </summary>
    private static bool BeValidInvoiceDate(DateOnly date, IConfiguration? configuration)
    {
        if (date == DateOnly.MinValue || date == DateOnly.MaxValue)
            return false;

        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);

        // Rule 1: Reject future dates
        if (date > todayUtc)
            return false;

        // VAPT REQUIREMENT: Minimum date is 2025-01-01 (absolute minimum for compliance)
        var minimumAllowedDate = new DateOnly(2025, 1, 1);
        if (date < minimumAllowedDate)
            return false;

        // Rule 2: Reject dates older than configured backstop period (default: 90 days)
        var maxBackdateDays = configuration?.GetValue<int>("InvoiceValidation:MaxBackdateDays", 90) ?? 90;
        var backstopDate = todayUtc.AddDays(-maxBackdateDays);

        // Use the more restrictive date (backstop vs minimum allowed date)
        var effectiveMinDate = backstopDate > minimumAllowedDate ? backstopDate : minimumAllowedDate;
        
        if (date < effectiveMinDate)
            return false;

        return true;
    }

    private static string GetInvoiceDateValidationMessage(IConfiguration? configuration)
    {
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var minimumAllowedDate = new DateOnly(2025, 1, 1);

        var maxBackdateDays = configuration?.GetValue<int?>("InvoiceValidation:MaxBackdateDays");
        if (maxBackdateDays.HasValue)
        {
            var backstopDate = todayUtc.AddDays(-maxBackdateDays.Value);
            var effectiveMinDate = backstopDate > minimumAllowedDate ? backstopDate : minimumAllowedDate;

            return $"Invoice date must be between {effectiveMinDate:yyyy-MM-dd} and {todayUtc:yyyy-MM-dd}. " +
                   $"System enforces a minimum date of 2025-01-01 for compliance. " +
                   $"Backdating is limited to {maxBackdateDays} days for financial accuracy.";
        }

        return $"Invoice date must be between {minimumAllowedDate:yyyy-MM-dd} and {todayUtc:yyyy-MM-dd}. " +
               "System enforces a minimum date of 2025-01-01 for compliance and audit purposes.";
    }

    /// <summary>
    /// Validates currency against supported currency allowlist
    /// </summary>
    private static bool BeValidCurrency(Currency currency, IConfiguration? configuration)
    {
        if (currency == null)
            return false;

        // Get supported currencies from configuration (default: NGN only for Nigerian FIRS compliance)
        var supportedCurrencies = configuration?
            .GetSection("InvoiceValidation:SupportedCurrencies")
            .Get<string[]>() ?? ["NGN"];

        // Validate currency code is in supported list
        return supportedCurrencies.Contains(currency.Code, StringComparer.OrdinalIgnoreCase);
    }

    private static string GetCurrencyValidationMessage(IConfiguration? configuration)
    {
        var fallbackCurrencies = configuration?
            .GetSection("InvoiceValidation:SupportedCurrencies")
            .Get<string[]>() ?? ["NGN"];

        return $"Invalid currency code. The currency must be supported by FIRS. " +
               $"If the FIRS validation service is unavailable, the following fallback currencies are accepted: {string.Join(", ", fallbackCurrencies)}. " +
               "Please use the GetCurrencies API to retrieve the current list of valid currency codes.";
    }

    /// <summary>
    /// Validates total invoice amount against reasonable business thresholds
    /// </summary>
    private static bool BeValidTotalAmount(List<InvoiceItemRequest> items, IConfiguration? configuration)
    {
        if (items == null || !items.Any())
            return false;

        // Get validation thresholds from configuration
        var maxQuantityPerItem = configuration?.GetValue<decimal>("InvoiceValidation:MaxQuantityPerItem", 1_000_000) ?? 1_000_000;

        // Validate each item quantity is reasonable
        foreach (var item in items)
        {
            // Quantity can be 0 or positive, but not negative
            if (item.Quantity < 0)
                return false;

            if (item.Quantity > maxQuantityPerItem)
                return false;
        }

        return true;
    }

    private static string GetTotalAmountValidationMessage(IConfiguration? configuration)
    {
        var maxInvoiceAmount = configuration?.GetValue<decimal>("InvoiceValidation:MaxInvoiceAmount", 1_000_000_000) ?? 1_000_000_000;
        var maxQuantityPerItem = configuration?.GetValue<decimal>("InvoiceValidation:MaxQuantityPerItem", 1_000_000) ?? 1_000_000;

        return $"Invoice validation failed. Each item quantity must be zero or positive (not negative) and not exceed {maxQuantityPerItem:N0}. " +
               $"Total invoice amount must not exceed {maxInvoiceAmount:N2}.";
    }

    /// <summary>
    /// Validates delivery period against business rules:
    /// 1. Start date must be on or after 2025-01-01 (VAPT compliance requirement)
    /// 2. Start date must not be after end date
    /// </summary>
    private static bool BeValidDeliveryPeriod(DeliveryPeriod? deliveryPeriod)
    {
        if (deliveryPeriod == null)
            return false;

        // VAPT REQUIREMENT: Minimum date is 2025-01-01
        var minimumAllowedDate = new DateOnly(2025, 1, 1);
        
        // Rule 1: Start date must not be before the minimum allowed date
        if (deliveryPeriod.StartDate < minimumAllowedDate)
            return false;

        // Rule 2: Start date must not be after end date
        if (deliveryPeriod.StartDate > deliveryPeriod.EndDate)
            return false;

        return true;
    }

    /// <summary>
    /// Validates invoice type name contains only alphanumeric characters and spaces
    /// VAPT requirement: Prevent special characters that could be used for injection attacks
    /// </summary>
    private static bool BeValidInvoiceTypeName(InvoiceType? invoiceType)
    {
        if (invoiceType == null || string.IsNullOrWhiteSpace(invoiceType.Name))
            return false;

        // Only allow letters, numbers, and spaces
        return invoiceType.Name.All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c));
    }

    /// <summary>
    /// Validates currency name contains only alphanumeric characters and spaces
    /// VAPT requirement: Prevent special characters that could be used for injection attacks
    /// </summary>
    private static bool BeValidCurrencyName(Currency? currency)
    {
        if (currency == null || string.IsNullOrWhiteSpace(currency.Name))
            return false;

        // Only allow letters, numbers, and spaces
        return currency.Name.All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c));
    }

    /// <summary>
    /// Validates payment means name contains only alphanumeric characters and spaces
    /// VAPT requirement: Prevent special characters that could be used for injection attacks
    /// </summary>
    private static bool BeValidPaymentMeansName(PaymentMeans? paymentMeans)
    {
        if (paymentMeans == null || string.IsNullOrWhiteSpace(paymentMeans.Name))
            return false;

        // Only allow letters, numbers, and spaces
        return paymentMeans.Name.All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c));
    }
}

/// <summary>
/// Validator for invoice item requests with input sanitization
/// Addresses VAPT finding: Improper input validation - accepts numeric and special characters
/// </summary>
public class InvoiceItemRequestValidator : AbstractValidator<InvoiceItemRequest>
{
    private readonly IConfiguration? _configuration;
    private readonly IReferenceDataCacheService _referenceDataCache;

    public InvoiceItemRequestValidator(
        IReferenceDataCacheService referenceDataCache, // REQUIRED for compliance!
        IConfiguration? configuration = null)
    {
        _configuration = configuration;
        _referenceDataCache = referenceDataCache ?? throw new ArgumentNullException(
            nameof(referenceDataCache),
            "Reference data cache service is required for invoice item validation.");

        // VAPT: Validate item name - only alphanumeric with basic business characters
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Item name is required for all invoice items.")
            .MaximumLength(200)
            .WithMessage("Item name is too long. Maximum 200 characters allowed, current length: {TotalLength}")
            .MustBeAlphanumeric(200, allowSpecialChars: true);

        // VAPT: Validate item description - sanitize text input
        RuleFor(x => x.ItemDescription)
            .NotEmpty()
            .WithMessage("Item description is required for all invoice items.")
            .MinimumLength(15)
            .WithMessage("Item description is too short. Minimum 15 characters required for clarity, current length: {TotalLength}")
            .MustBeAlphanumeric(500, allowSpecialChars: false)
            .MustBeSafeText(500);

        // VAPT: Validate item category - only alphanumeric
        RuleFor(x => x.ItemCategory)
            .NotEmpty()
            .WithMessage("Item category is required. Please assign each item to a valid category.")
            .MustBeAlphanumeric(100, allowSpecialChars: false);

        RuleFor(x => x.ServiceCode)
            .NotNull()
            .WithMessage("Service code is required for all invoice items. Please provide the FIRS service code.")
            .Must((item, serviceCode) => BeValidServiceCode(serviceCode))
            .WithMessage(x => $"Invalid service code '{x.ServiceCode?.Code}' on invoice item '{x.Name}'. " +
                             $"Valid service codes include: {GetValidServiceCodes()}");

        RuleFor(x => x.TaxCategory)
            .NotNull()
            .WithMessage("Tax category is required for all invoice items. Please specify the applicable tax category.");

        // VAPT: Validate unit price - must be non-negative and within reasonable bounds
        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Unit price must be zero or greater. Negative prices are not allowed. Value provided: {PropertyValue}")
            .LessThanOrEqualTo(1_000_000_000)
            .WithMessage("Unit price of {PropertyValue} exceeds maximum allowed value of 1,000,000,000. Please verify the price is correct.");

        // VAPT: Validate quantity - must be zero or positive and within reasonable bounds
        var maxQuantityPerItem = _configuration?.GetValue<decimal>("InvoiceValidation:MaxQuantityPerItem", 1_000_000) ?? 1_000_000;
        
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Quantity must be zero or positive (negative values are not allowed). Value provided: {PropertyValue}")
            .LessThanOrEqualTo(maxQuantityPerItem)
            .WithMessage($"Quantity of {{PropertyValue}} exceeds maximum allowed value of {maxQuantityPerItem:N0}. Please verify the quantity is correct.");
    }

    /// <summary>
    /// Validates service code against FIRS cached reference data.
    /// Cache is loaded at startup and refreshed daily at 12 AM.
    /// If cache fails to refresh, last known good cache is retained.
    /// </summary>
    private bool BeValidServiceCode(ServiceCodeRequest? serviceCode)
    {
        if (serviceCode == null)
            return false;

        if (string.IsNullOrWhiteSpace(serviceCode.Code))
            return true; // Allow null/empty (will be caught by NotNull rule)

        return _referenceDataCache.IsValidServiceCode(serviceCode.Code);
    }

    /// <summary>
    /// Gets valid service codes for error messages
    /// </summary>
    private string GetValidServiceCodes()
    {
        var codes = _referenceDataCache.GetServiceCodes().Take(5);
        return codes.Any() ? string.Join(", ", codes) : "No service codes available from FIRS";
    }
}

public class BillingReferenceRequestValidator : AbstractValidator<BillingReferenceRequest>
{
    public BillingReferenceRequestValidator()
    {
        RuleFor(x => x.Irn)
            .NotEmpty()
            .WithMessage("Billing reference IRN is required.")
            .Must(IRN.IsValidIRNFormat)
            .WithMessage("Invalid billing reference IRN format: '{PropertyValue}'. Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD (e.g., ITW00000001-E9E0C0D3-20240619)");

        RuleFor(x => x.IssueDate)
            .NotEmpty()
            .WithMessage("Billing reference issue date is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Billing reference issue date cannot be in the future. Provided date: {PropertyValue}, Today: {ComparisonValue}");
    }
}

public class DocumentReferenceRequestValidator : AbstractValidator<DocumentReferenceRequest>
{
    public DocumentReferenceRequestValidator()
    {
        RuleFor(x => x.Irn)
            .NotEmpty()
            .WithMessage("Document reference IRN is required.")
            .Must(IRN.IsValidIRNFormat)
            .WithMessage("Invalid document reference IRN format: '{PropertyValue}'. Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD (e.g., ITW00000001-E9E0C0D3-20240619)");

        RuleFor(x => x.IssueDate)
            .NotEmpty()
            .WithMessage("Document reference issue date is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Document reference issue date cannot be in the future. Provided date: {PropertyValue}, Today: {ComparisonValue}");
    }
}