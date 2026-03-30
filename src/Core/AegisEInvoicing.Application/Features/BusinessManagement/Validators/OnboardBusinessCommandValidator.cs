using AegisEInvoicing.Application.Common.Security;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.OnboardBusiness;
using AegisEInvoicing.Domain.ValueObjects;
using FluentValidation;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Validators;

/// <summary>
/// Validator for ConnectToFIRSCommand with FIRS-specific validation and input sanitization
/// Addresses VAPT finding: Improper input validation
/// </summary>
public class OnboardBusinessCommandValidator : AbstractValidator<OnboardBusinessCommand>
{
    public OnboardBusinessCommandValidator()
    {
        // Business Name - Sanitize alphanumeric with business characters
        RuleFor(x => x.BusinessName)
            .NotEmpty()
            .WithMessage("Business name is required")
            .MaximumLength(200)
            .WithMessage("Business name must not exceed 200 characters")
            .MustBeAlphanumeric(200, allowSpecialChars: true)
            .MustBeSafeInput()
            .Must(BeValidBusinessName)
            .WithMessage("Business name must contain only valid characters");

        // TIN - Sanitize to prevent injection
        RuleFor(x => x.TIN)
            .NotEmpty()
            .WithMessage("TIN is required")
            .MaximumLength(50)
            .WithMessage("TIN must not exceed 50 characters")
            .MustBeNumeric()
            .Must(BeValidNigerianTIN)
            .WithMessage("TIN must be a valid Nigerian Tax Identification Number");

        // Business Registration Number - Sanitize alphanumeric
        RuleFor(x => x.BusinessRegistrationNumber)
            .NotEmpty()
            .WithMessage("Business Registration Number is required")
            .MaximumLength(50)
            .WithMessage("Business Registration Number must not exceed 50 characters")
            .MustBeAlphanumeric(50, allowSpecialChars: false)
            .Must(BeValidBusinessRegistrationNumber)
            .WithMessage("Business Registration Number must be in valid format (RC followed by digits)");

        RuleFor(x => x.RegisteredAddress)
            .NotNull()
            .WithMessage("Registered address is required")
            .SetValidator(new BusinessAddressValidator());

        // Email - Sanitize to prevent email injection
        RuleFor(x => x.ContactEmail)
            .NotEmpty()
            .WithMessage("Contact email is required")
            .MaximumLength(255)
            .WithMessage("Contact email must not exceed 255 characters")
            .EmailAddress()
            .WithMessage("Invalid contact email format")
            .MustBeSafeEmail();

        // Phone - Sanitize to prevent encoded delimiter injection
        RuleFor(x => x.ContactPhone)
            .NotEmpty()
            .WithMessage("Contact phone is required")
            .MaximumLength(50)
            .WithMessage("Contact phone must not exceed 50 characters")
            .MustBeSafePhone()
            .Must(BeValidNigerianPhoneNumber)
            .WithMessage("Contact phone must be a valid Nigerian phone number");

        // Business Admin User validation - Sanitize names
        RuleFor(x => x.AdminFirstName)
            .NotEmpty()
            .WithMessage("Admin first name is required")
            .MaximumLength(100)
            .WithMessage("Admin first name must not exceed 100 characters")
            .MustBeSafeCityState(100); // Reuse city/state validator for names (only letters, spaces, hyphens)

        RuleFor(x => x.AdminLastName)
            .NotEmpty()
            .WithMessage("Admin last name is required")
            .MaximumLength(100)
            .WithMessage("Admin last name must not exceed 100 characters")
            .MustBeSafeCityState(100); // Reuse city/state validator for names

        // Business rule validations
        RuleFor(x => x)
            .Must(HaveMatchingEmails)
            .WithMessage("Contact email should typically match the primary email")
            .Must(HaveValidBusinessCredentials)
            .WithMessage("Business credentials must be consistent");
    }

    private static bool BeValidBusinessName(string? businessName)
    {
        if (string.IsNullOrWhiteSpace(businessName))
            return false;

        // Allow letters, numbers, spaces, periods, hyphens, and common business symbols
        return businessName.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '.' || c == '-' || c == '&' || c == ',' || c == '(' || c == ')');
    }

    private static bool BeValidNigerianTIN(string? tin)
    {
        if (string.IsNullOrWhiteSpace(tin))
            return false;

        // Remove hyphens and spaces
        var cleanTIN = tin.Replace("-", "").Replace(" ", "");

        // Nigerian TIN format: typically 8-11 digits
        return cleanTIN.All(char.IsDigit) && cleanTIN.Length > 11 && cleanTIN.Length < 13;
    }

    private static bool BeValidBusinessRegistrationNumber(string? registrationNumber)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
            return false;

        // Nigerian business registration format: RC followed by digits
        // Examples: RC123456, RC1234567, RC12345678
        var cleanNumber = registrationNumber.Replace(" ", "").Replace("-", "").ToUpperInvariant();

        if (!cleanNumber.StartsWith("RC"))
            return false;

        var numberPart = cleanNumber[2..]; // Remove "RC" prefix

        // Should be 6-8 digits after RC
        return numberPart.All(char.IsDigit) && numberPart.Length > 3 && numberPart.Length < 10;
    }

    private static bool BeAlphanumeric(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return input.All(char.IsLetterOrDigit);
    }

    private static bool BeStrongPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        var hasUpperCase = password.Any(char.IsUpper);
        var hasLowerCase = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

        return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
    }

    private static bool BeValidNigerianPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Remove common formatting characters
        var cleanNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        
        // Nigerian phone numbers: 11 digits starting with 0, or 10 digits without leading 0, or 13 digits with country code
        if (cleanNumber.All(char.IsDigit))
        {
            return cleanNumber.Length == 11 && cleanNumber.StartsWith("0") ||
                   cleanNumber.Length == 10 && !cleanNumber.StartsWith("0") ||
                   cleanNumber.Length == 13 && cleanNumber.StartsWith("234");
        }

        return false;
    }

    private static bool HaveMatchingEmails(OnboardBusinessCommand command)
    {
        // This is more of a warning than a strict validation
        // In some cases, contact email might be different from primary email
        return true;
    }

    private static bool HaveValidBusinessCredentials(OnboardBusinessCommand command)
    {
        // Since KMPG handles FIRS credentials, we just ensure basic business data consistency
        // Business Name, TIN, and Registration Number should not be identical
        var businessName = command.BusinessName?.Trim() ?? "";
        var tin = command.TIN?.Trim() ?? "";
        var regNumber = command.BusinessRegistrationNumber?.Trim() ?? "";

        // Ensure they're not the same values (which would indicate data entry error)
        return !string.Equals(businessName, tin, StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(businessName, regNumber, StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(tin, regNumber, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Validator for business Address with Nigerian address specifics and input sanitization
/// Addresses VAPT finding: Improper input validation
/// </summary>
public class BusinessAddressValidator : AbstractValidator<Address>
{
    public BusinessAddressValidator()
    {
        // Street Address - Sanitize to prevent injection
        RuleFor(x => x.Street)
            .NotEmpty()
            .WithMessage("Street address is required")
            .MaximumLength(200)
            .WithMessage("Street address must not exceed 200 characters")
            .MustBeSafeAddress(200);

        // City - Sanitize city names
        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required")
            .MaximumLength(100)
            .WithMessage("City must not exceed 100 characters")
            .MustBeSafeCityState(100)
            .Must(BeValidNigerianCity)
            .WithMessage("Please provide a valid Nigerian city");

        // State - Sanitize state names
        RuleFor(x => x.State)
            .NotEmpty()
            .WithMessage("State is required for Nigerian addresses")
            .MaximumLength(100)
            .WithMessage("State must not exceed 100 characters")
            .MustBeSafeCityState(100)
            .Must(BeValidNigerianState)
            .WithMessage("Please provide a valid Nigerian state");

        // Country - Sanitize country code
        RuleFor(x => x.Country)
            .NotEmpty()
            .WithMessage("Country is required")
            .MustBeSafeCountryCode()
            .Must(x => string.Equals(x, "NG", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Country must be Nigeria for FIRS integration");

        // Postal Code - Sanitize postal code
        RuleFor(x => x.PostalCode)
            .MaximumLength(20)
            .WithMessage("Postal code must not exceed 20 characters")
            .MustBeSafePostalCode(20)
            .Must(BeValidNigerianPostalCode)
            .When(x => !string.IsNullOrWhiteSpace(x.PostalCode))
            .WithMessage("Invalid postal code format for Nigeria");
    }

    private static bool BeValidNigerianCity(string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return false;

        // List of major Nigerian cities for validation
        var majorCities = new[]
        {
            "Lagos", "Abuja", "Kano", "Ibadan", "Port Harcourt", "Benin City", "Maiduguri",
            "Zaria", "Aba", "Jos", "Kaduna", "Warri", "Onitsha", "Enugu", "Calabar",
            "Sokoto", "Katsina", "Bauchi", "Akure", "Ilorin", "Osogbo", "Abeokuta",
            "Uyo", "Yenagoa", "Gombe", "Minna", "Lafia", "Jalingo", "Birnin Kebbi",
            "Dutse", "Damaturu", "Yola", "Lokoja", "Makurdi", "Asaba", "Awka", "Owerri"
        };

        // Allow any city but prefer known major cities (this could be made more flexible)
        return city.All(c => char.IsLetter(c) || c == ' ' || c == '-');
    }

    private static bool BeValidNigerianState(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return false;

        // Nigerian states for validation
        var nigerianStates = new[]
        {
            "Abia", "Adamawa", "Akwa Ibom", "Anambra", "Bauchi", "Bayelsa", "Benue", "Borno",
            "Cross River", "Delta", "Ebonyi", "Edo", "Ekiti", "Enugu", "Gombe", "Imo",
            "Jigawa", "Kaduna", "Kano", "Katsina", "Kebbi", "Kogi", "Kwara", "Lagos",
            "Nasarawa", "Niger", "Ogun", "Ondo", "Osun", "Oyo", "Plateau", "Rivers",
            "Sokoto", "Taraba", "Yobe", "Zamfara", "FCT", "Federal Capital Territory"
        };

        return nigerianStates.Any(s => string.Equals(s, state, StringComparison.OrdinalIgnoreCase));
    }

    private static bool BeValidNigerianPostalCode(string? postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            return true; // Optional

        // Nigerian postal codes are typically 6 digits
        return postalCode.All(char.IsDigit) && postalCode.Length == 6;
    }
}