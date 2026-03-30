using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Security;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoice;

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    private readonly IConfiguration? _configuration;
    private readonly IFIRSCurrencyValidationService? _fIRSCurrencyValidationService;

    public CreateInvoiceCommandValidator(IConfiguration? configuration = null,
        IFIRSCurrencyValidationService? fIRSCurrencyValidationService = null)
    {
        _configuration = configuration;
        _fIRSCurrencyValidationService = fIRSCurrencyValidationService;

        RuleFor(x => x.IssueDate)
            .NotEmpty()
            .WithMessage("Issue date is required")
            .Must(date => BeValidInvoiceDate(date, _configuration))
            .WithMessage(x => GetInvoiceDateValidationMessage(_configuration));

        RuleFor(x => x.InvoiceType)
            .NotNull()
            .WithMessage("Invoice type is required")
            .Must(invoiceType => BeValidInvoiceTypeName(invoiceType))
            .WithMessage("Invoice type name must contain only letters, numbers, and spaces (no special characters allowed)");

        RuleFor(x => x.Currency)
            .NotNull()
            .WithMessage("Currency is required")
            .Must(currency => BeValidCurrencyName(currency))
            .WithMessage("Currency name must contain only letters, numbers, and spaces (no special characters allowed)")
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
           .WithMessage("Payment Means is required")
           .Must(paymentMeans => BeValidPaymentMeansName(paymentMeans))
           .WithMessage("Payment means name must contain only letters, numbers, and spaces (no special characters allowed)");

        RuleFor(x => x.InvoiceItems)
           .NotEmpty()
           .WithMessage("At least one invoice item is required")
           .Must(HaveUniqueItems)
           .WithMessage("Invoice items must be unique");

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.IssueDate)
            .WithMessage("Due date must be on or after the issue date")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .WithMessage("Note must not exceed 500 characters")
            .MustBeSafeText(500)
            .When(x => !string.IsNullOrEmpty(x.Note));

        RuleFor(x => x.PaymentReference)
            .MaximumLength(100)
            .WithMessage("Payment reference must not exceed 100 characters")
            .MustBeAlphanumeric(100, allowSpecialChars: true)
            .When(x => !string.IsNullOrEmpty(x.PaymentReference));

        RuleFor(x => x.PaymentTerms)
            .MaximumLength(200)
            .WithMessage("Payment terms must not exceed 200 characters")
            .MustBeSafeText(200)
            .When(x => !string.IsNullOrEmpty(x.PaymentTerms));

        RuleForEach(x => x.InvoiceItems)
           .SetValidator(new CreateInvoiceItemDtoValidator());

        RuleFor(x => x.InvoiceItems)
            .NotEmpty()
            .WithMessage("At least one invoice item is required")
            .Must(HaveUniqueItems)
            .WithMessage("Invoice items must be unique")
            .Must(items => BeValidTotalAmount(items, _configuration))
            .WithMessage(x => GetTotalAmountValidationMessage(_configuration));

        RuleFor(x => x.DeliveryPeriod)
            .NotNull()
            .WithMessage("Delivery period is required")
            .Must(deliveryPeriod => BeValidDeliveryPeriod(deliveryPeriod))
            .WithMessage("Delivery period start date must be on or after 2025-01-01 and cannot be after the end date");

        RuleForEach(x => x.BillingReferences)
            .SetValidator(new CreateBillingReferenceDtoValidator())
            .When(x => x.BillingReferences != null && x.BillingReferences.Any());

        // Document reference validators
        RuleFor(x => x.DispatchDocumentReference)
            .SetValidator(new CreateDocumentReferenceDtoValidator()!)
            .When(x => x.DispatchDocumentReference != null);

        RuleFor(x => x.ReceiptDocumentReference)
            .SetValidator(new CreateDocumentReferenceDtoValidator()!)
            .When(x => x.ReceiptDocumentReference != null);

        RuleFor(x => x.OriginatorDocumentReference)
            .SetValidator(new CreateDocumentReferenceDtoValidator()!)
            .When(x => x.OriginatorDocumentReference != null);

        RuleFor(x => x.ContractDocumentReference)
            .SetValidator(new CreateDocumentReferenceDtoValidator()!)
            .When(x => x.ContractDocumentReference != null);

        RuleForEach(x => x.AdditionalDocumentReferences)
            .SetValidator(new CreateDocumentReferenceDtoValidator())
            .When(x => x.AdditionalDocumentReferences != null && x.AdditionalDocumentReferences.Any());
    }

    /// <summary>
    /// Validates invoice date against business rules:
    /// 1. Not in the future
    /// 2. Not older than 2025-01-01 (VAPT compliance requirement - absolute minimum)
    /// 3. Not older than configured backstop period (default: 90 days from today)
    /// 4. Not older than 2 years (absolute limit for audit/compliance)
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
    private static bool BeValidTotalAmount(List<CreateInvoiceItemDto> items, IConfiguration? configuration)
    {
        if (items == null || !items.Any())
            return false;

        // Get validation thresholds from configuration
        var maxInvoiceAmount = configuration?.GetValue<decimal>("InvoiceValidation:MaxInvoiceAmount", 1_000_000_000) ?? 1_000_000_000; // 1 billion default
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

    private static bool HaveUniqueItems(List<CreateInvoiceItemDto> items)
    {
        if (items == null || items.Count == 0)
            return false;

        var uniqueItems = items.Select(i => new { i.BusinessItemId })
            .Distinct()
            .Count();

        return uniqueItems == items.Count;
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

public class CreateInvoiceItemDtoValidator : AbstractValidator<CreateInvoiceItemDto>
{
    public CreateInvoiceItemDtoValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Invoiced quantity must be zero or positive (negative values are not allowed)");

        RuleFor(x => x.BusinessItemId)
            .NotEmpty()
            .WithMessage("Business item ID is required");
    }
}

public class PartyDtoValidator : AbstractValidator<PartyDto>
{
    public PartyDtoValidator()
    {
        RuleFor(x => x.Tin.Value)
            .NotEmpty()
            .WithMessage("Party tax scheme is required")
            .Must(IsValidNigerianTIN)
            .WithMessage("Invalid Tax Identification Number Used")
            .MustBeSafeTIN();

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Party legal entity is required")
            .MaximumLength(200)
            .WithMessage("Party legal entity must not exceed 200 characters")
            .MustBeAlphanumeric(200, allowSpecialChars: true);

        RuleFor(x => x.Address)
            .NotNull()
            .WithMessage("Address is required")
            .SetValidator(new PostalAddressDtoValidator());

        RuleFor(x => x.Email)
            .NotNull()
            .WithMessage("Email is required")
            .MustBeSafeEmail();

        RuleFor(x => x.Phone)
            .NotNull()
            .WithMessage("Phone is required")
            .MustBeSafePhone();
    }

    private static bool IsValidNigerianTIN(string tin)
    {
        if (string.IsNullOrWhiteSpace(tin))
            return false;

        return true;

        // Seun 13/01/2026: Kehinde said Relaxing TIN validation to allow flexibility for different formats
        //if(!tin.Contains('-'))
        //    return false;

        //var tinValue = tin.Split('-');
        //if(tinValue.Length < 2 || tinValue.Length > 2) 
        //    return false;

        //var cleanTin = tin.Trim().Replace("-", "").Replace(" ", "");

        //return cleanTin.Length == 12 && cleanTin.All(char.IsDigit);
    }
}

public class PostalAddressDtoValidator : AbstractValidator<Address>
{
    public PostalAddressDtoValidator()
    {
        RuleFor(x => x.Street)
            .NotEmpty()
            .WithMessage("Street is required")
            .MaximumLength(200)
            .WithMessage("Street must not exceed 200 characters")
            .MustBeSafeAddress(200);

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required")
            .MaximumLength(100)
            .WithMessage("City must not exceed 100 characters")
            .MustBeSafeCityState(100);

        RuleFor(x => x.State)
            .NotEmpty()
            .WithMessage("State is required")
            .MaximumLength(100)
            .WithMessage("State must not exceed 100 characters")
            .MustBeSafeCityState(100);

        RuleFor(x => x.Country)
            .NotEmpty()
            .WithMessage("Country code is required")
            .Length(2)
            .WithMessage("Country code must be 2 characters (ISO 3166-1 alpha-2)")
            .Matches("^[A-Z]{2}$")
            .WithMessage("Country code must be 2 uppercase letters")
            .MustBeSafeCountryCode();

        RuleFor(x => x.PostalCode)
            .MaximumLength(20)
            .WithMessage("Postal zone must not exceed 20 characters")
            .MustBeSafePostalCode(20)
            .When(x => !string.IsNullOrEmpty(x.PostalCode));
    }
}

public class PaymentMeansDtoValidator : AbstractValidator<PaymentMeans>
{
    public PaymentMeansDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Payment means code is required")
            .MaximumLength(10)
            .WithMessage("Payment means code must not exceed 10 characters")
            .MustBeAlphanumeric(10, allowSpecialChars: false);

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Payment means name is required")
            .MaximumLength(100)
            .WithMessage("Payment means name must not exceed 100 characters")
            .Must(name => name.All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)))
            .WithMessage("Payment means name must contain only letters, numbers, and spaces (no special characters allowed)");
    }
}

public class CreateBillingReferenceDtoValidator : AbstractValidator<CreateBillingReferenceDto>
{
    public CreateBillingReferenceDtoValidator()
    {
        RuleFor(x => x.Irn)
            .NotEmpty()
            .WithMessage("Billing reference IRN is required")
            .Must(IRN.IsValidIRNFormat)
            .WithMessage("Invalid IRN format. Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD (e.g., ITW00000001-E9E0C0D3-20240619)");

        RuleFor(x => x.IssueDate)
            .NotEmpty()
            .WithMessage("Billing reference issue date is required")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Billing reference issue date cannot be in the future");
    }
}

public class CreateDocumentReferenceDtoValidator : AbstractValidator<CreateDocumentReferenceDto>
{
    public CreateDocumentReferenceDtoValidator()
    {
        RuleFor(x => x.Irn)
            .NotEmpty()
            .WithMessage("Document reference IRN is required")
            .Must(IRN.IsValidIRNFormat)
            .WithMessage("Invalid IRN format. Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD (e.g., ITW00000001-E9E0C0D3-20240619)");

        RuleFor(x => x.IssueDate)
            .NotEmpty()
            .WithMessage("Document reference issue date is required")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Document reference issue date cannot be in the future");
    }
}