using AegisEInvoicing.Application.Common.Security;
using AegisEInvoicing.ERP.API.Models;
using FluentValidation;

namespace AegisEInvoicing.ERP.API.Validators;

/// <summary>
/// Validator for CreateInvoiceRequest with comprehensive input sanitization
/// Addresses VAPT finding: Improper input validation - accepts numeric and special characters
/// </summary>
public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.AegisBusinessId)
            .NotEmpty()
            .WithMessage("Business ID is required");

        RuleFor(x => x.InvoiceNumber)
            .MaximumLength(50)
            .WithMessage("Invoice number must not exceed 50 characters")
            .MustBeAlphanumeric(50, allowSpecialChars: true)
            .When(x => !string.IsNullOrEmpty(x.InvoiceNumber));

        RuleFor(x => x.IssueDate)
            .NotEmpty()
            .WithMessage("Issue date is required")
            .Must(BeValidDate)
            .WithMessage("Issue date cannot be in the future");

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.IssueDate)
            .WithMessage("Due date must be on or after the issue date")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.InvoiceType)
            .NotNull()
            .WithMessage("Invoice type is required")
            .SetValidator(new InvoiceTypeRequestValidator());

        RuleFor(x => x.Currency)
            .NotNull()
            .WithMessage("Currency is required")
            .SetValidator(new CurrencyRequestValidator());

        RuleFor(x => x.DeliveryPeriod)
            .NotNull()
            .WithMessage("Delivery period is required")
            .SetValidator(new DeliveryPeriodRequestValidator());

        RuleFor(x => x.PaymentMeans)
            .NotNull()
            .WithMessage("Payment means is required")
            .SetValidator(new PaymentMeansRequestValidator());

        // VAPT: Sanitize note field
        RuleFor(x => x.Note)
            .MaximumLength(1000)
            .WithMessage("Note must not exceed 1000 characters")
            .MustBeSafeText(1000)
            .When(x => !string.IsNullOrEmpty(x.Note));

        // VAPT: Sanitize payment reference
        RuleFor(x => x.PaymentReference)
            .MaximumLength(100)
            .WithMessage("Payment reference must not exceed 100 characters")
            .MustBeAlphanumeric(100, allowSpecialChars: true)
            .When(x => !string.IsNullOrEmpty(x.PaymentReference));

        // VAPT: Sanitize payment terms
        RuleFor(x => x.PaymentTerms)
            .MaximumLength(500)
            .WithMessage("Payment terms must not exceed 500 characters")
            .MustBeSafeText(500)
            .When(x => !string.IsNullOrEmpty(x.PaymentTerms));

        RuleFor(x => x.Party)
            .NotNull()
            .WithMessage("Party information is required")
            .SetValidator(new PartyRequestValidator());

        RuleFor(x => x.InvoiceItems)
            .NotEmpty()
            .WithMessage("At least one invoice item is required");

        RuleForEach(x => x.InvoiceItems)
            .SetValidator(new CreateInvoiceItemDtoValidator());
    }

    private static bool BeValidDate(DateOnly date)
    {
        if (date == DateOnly.MinValue || date == DateOnly.MaxValue)
            return false;

        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        return date <= todayUtc;
    }
}

/// <summary>
/// Validator for InvoiceTypeRequest
/// </summary>
public class InvoiceTypeRequestValidator : AbstractValidator<InvoiceTypeRequest>
{
    public InvoiceTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Invoice type name is required")
            .MaximumLength(100)
            .WithMessage("Invoice type name must not exceed 100 characters")
            .MustBeAlphanumeric(100, allowSpecialChars: false);

        RuleFor(x => x.Code)
            .GreaterThan(0)
            .WithMessage("Invoice type code must be positive")
            .LessThanOrEqualTo(999)
            .WithMessage("Invoice type code must be a valid 3-digit code");
    }
}

/// <summary>
/// Validator for CurrencyRequest
/// </summary>
public class CurrencyRequestValidator : AbstractValidator<CurrencyRequest>
{
    public CurrencyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Currency name is required")
            .MaximumLength(50)
            .WithMessage("Currency name must not exceed 50 characters")
            .MustBeAlphanumeric(50, allowSpecialChars: false);

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Currency code is required")
            .MustBeSafeCurrencyCode();
    }
}

/// <summary>
/// Validator for DeliveryPeriodRequest
/// </summary>
public class DeliveryPeriodRequestValidator : AbstractValidator<DeliveryPeriodRequest>
{
    public DeliveryPeriodRequestValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Delivery start date is required");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("Delivery end date is required")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Delivery end date must be on or after start date");
    }
}

/// <summary>
/// Validator for PaymentMeansRequest
/// </summary>
public class PaymentMeansRequestValidator : AbstractValidator<PaymentMeansRequest>
{
    public PaymentMeansRequestValidator()
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
            .MustBeAlphanumeric(100, allowSpecialChars: true);
    }
}

/// <summary>
/// Validator for PartyRequest with comprehensive input sanitization
/// </summary>
public class PartyRequestValidator : AbstractValidator<PartyRequest>
{
    public PartyRequestValidator()
    {
        // VAPT: Validate party name - only alphanumeric with basic business characters
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Party name is required")
            .MaximumLength(200)
            .WithMessage("Party name must not exceed 200 characters")
            .MustBeAlphanumeric(200, allowSpecialChars: true);

        // VAPT: Validate description - sanitize text input
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Party description is required")
            .MaximumLength(2000)
            .WithMessage("Description must not exceed 2000 characters")
            .MustBeSafeText(2000);

        // VAPT: Validate phone - only valid phone characters
        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("Phone number is required")
            .MustBeSafePhone();

        // VAPT: Validate email - only valid email format
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email address is required")
            .MustBeSafeEmail();

        // VAPT: Validate TIN - only digits and hyphens
        RuleFor(x => x.TaxIdentificationNumber)
            .NotEmpty()
            .WithMessage("Tax identification number is required")
            .MustBeSafeTIN();

        RuleFor(x => x.Address)
            .NotNull()
            .WithMessage("Address is required")
            .SetValidator(new AddressRequestValidator());
    }
}

/// <summary>
/// Validator for AddressRequest with comprehensive input sanitization
/// </summary>
public class AddressRequestValidator : AbstractValidator<AddressRequest>
{
    public AddressRequestValidator()
    {
        // VAPT: Validate street address
        RuleFor(x => x.Street)
            .NotEmpty()
            .WithMessage("Street address is required")
            .MaximumLength(200)
            .WithMessage("Street must not exceed 200 characters")
            .MustBeSafeAddress(200);

        // VAPT: Validate city - only letters, spaces, hyphens
        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required")
            .MaximumLength(100)
            .WithMessage("City must not exceed 100 characters")
            .MustBeSafeCityState(100);

        // VAPT: Validate state - only letters, spaces, hyphens
        RuleFor(x => x.State)
            .NotEmpty()
            .WithMessage("State is required")
            .MaximumLength(100)
            .WithMessage("State must not exceed 100 characters")
            .MustBeSafeCityState(100);

        // VAPT: Validate country code
        RuleFor(x => x.Country)
            .NotEmpty()
            .WithMessage("Country is required")
            .MustBeSafeCountryCode();

        // VAPT: Validate postal code - alphanumeric with spaces and hyphens
        RuleFor(x => x.PostalCode)
            .MaximumLength(20)
            .WithMessage("Postal code must not exceed 20 characters")
            .MustBeSafePostalCode(20)
            .When(x => !string.IsNullOrEmpty(x.PostalCode));
    }
}

/// <summary>
/// Validator for CreateInvoiceItemDto with comprehensive input sanitization
/// </summary>
public class CreateInvoiceItemDtoValidator : AbstractValidator<CreateInvoiceItemDto>
{
    public CreateInvoiceItemDtoValidator()
    {
        // VAPT: Validate item name - only alphanumeric with basic business characters
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Item name is required")
            .MaximumLength(200)
            .WithMessage("Item name must not exceed 200 characters")
            .MustBeAlphanumeric(200, allowSpecialChars: true);

        // VAPT: Validate item description - sanitize text input
        RuleFor(x => x.ItemDescription)
            .NotEmpty()
            .WithMessage("Item description is required")
            .MaximumLength(500)
            .WithMessage("Item description must not exceed 500 characters")
            .MustBeSafeText(500);

        // VAPT: Validate item category - only alphanumeric
        RuleFor(x => x.ItemCategory)
            .NotEmpty()
            .WithMessage("Item category is required")
            .MustBeAlphanumeric(100, allowSpecialChars: false);

        RuleFor(x => x.ServiceCode)
            .NotNull()
            .WithMessage("Service code is required")
            .SetValidator(new ServiceCodeRequestValidator());

        RuleFor(x => x.TaxCategory)
            .NotNull()
            .WithMessage("Tax category is required")
            .SetValidator(new TaxCategoryRequestValidator());

        // VAPT: Validate unit price - must be non-negative and within reasonable bounds
        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Unit price must be greater than or equal to zero")
            .LessThanOrEqualTo(1_000_000_000)
            .WithMessage("Unit price exceeds maximum allowed value (1 billion)");

        // VAPT: Validate quantity - must be positive and within reasonable bounds
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Quantity exceeds maximum allowed value (1 million)");
    }
}

/// <summary>
/// Validator for ServiceCodeRequest with input sanitization
/// </summary>
public class ServiceCodeRequestValidator : AbstractValidator<ServiceCodeRequest>
{
    public ServiceCodeRequestValidator()
    {
        // VAPT: Validate service code - only alphanumeric
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Service code is required")
            .MaximumLength(50)
            .WithMessage("Service code must not exceed 50 characters")
            .MustBeAlphanumeric(50, allowSpecialChars: false);

        // VAPT: Validate service name - alphanumeric with spaces
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Service code name is required")
            .MaximumLength(200)
            .WithMessage("Service code name must not exceed 200 characters")
            .MustBeAlphanumeric(200, allowSpecialChars: true);
    }
}

/// <summary>
/// Validator for TaxCategoryRequest with input sanitization
/// </summary>
public class TaxCategoryRequestValidator : AbstractValidator<TaxCategoryRequest>
{
    public TaxCategoryRequestValidator()
    {
        // VAPT: Validate tax category name - only alphanumeric with basic characters
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Tax category name is required")
            .MaximumLength(500)
            .WithMessage("Tax category name must not exceed 500 characters")
            .MustBeAlphanumeric(500, allowSpecialChars: true);

        // VAPT: Validate tax percentage - must be within valid range
        RuleFor(x => x.Percent)
            .InclusiveBetween(0, 100)
            .WithMessage("Tax percentage must be between 0 and 100");
    }
}
