using AegisEInvoicing.Application.Common.Security;
using FluentValidation;

namespace AegisEInvoicing.Application.Features.PartyManagement.Commands.CreateParty;

/// <summary>
/// Validator for CreatePartyCommand with comprehensive input sanitization
/// Addresses VAPT finding: Improper input validation
/// </summary>
public class CreatePartyCommandValidator : AbstractValidator<CreatePartyCommand>
{
    public CreatePartyCommandValidator()
    {
        // Party Name - Sanitize alphanumeric with business characters
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Party name is required")
            .MaximumLength(200)
            .WithMessage("Party name must not exceed 200 characters")
            .MustBeAlphanumeric(200, allowSpecialChars: true)
            .MustBeSafeInput();

        // Phone - Sanitize to prevent encoded delimiter injection
        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("Phone number is required")
            .MaximumLength(50)
            .WithMessage("Phone number must not exceed 50 characters")
            .MustBeSafePhone()
            .Must(BeValidPhoneNumber)
            .WithMessage("Invalid phone number format");

        // Description - Sanitize to prevent XSS and injection attacks
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Party Description is required")
            .MaximumLength(2000)
            .WithMessage("Party Description must not exceed 2000 characters")
            .MustBeSafeText(2000);

        // Email - Sanitize to prevent email injection
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(255)
            .WithMessage("Email must not exceed 255 characters")
            .MustBeSafeEmail();

        // TIN - Sanitize to prevent injection
        RuleFor(x => x.TaxIdentificationNumber)
            .NotEmpty()
            .WithMessage("Tax identification number is required")
            .MustBeSafeTIN()
            .Must(BeValidNigerianTIN)
            .WithMessage("Invalid Nigerian TIN format. TIN must be 12 digits in format XXX-XXX-XXX");

        RuleFor(x => x.Address)
            .NotNull()
            .WithMessage("Address is required");

        When(x => x.Address != null, () =>
        {
            // Street Address - Sanitize to prevent injection
            RuleFor(x => x.Address.Street)
                .NotEmpty()
                .WithMessage("Street address is required")
                .MaximumLength(500)
                .WithMessage("Street address must not exceed 500 characters")
                .MustBeSafeAddress(500);

            // City - Sanitize city/state names
            RuleFor(x => x.Address.City)
                .NotEmpty()
                .WithMessage("City is required")
                .MaximumLength(100)
                .WithMessage("City must not exceed 100 characters")
                .MustBeSafeCityState(100);

            // State - Sanitize city/state names
            RuleFor(x => x.Address.State)
                .NotEmpty()
                .WithMessage("State is required")
                .MaximumLength(100)
                .WithMessage("State must not exceed 100 characters")
                .MustBeSafeCityState(100);

            // Country - Sanitize country code
            RuleFor(x => x.Address.Country)
                .NotEmpty()
                .WithMessage("Country is required")
                .MaximumLength(100)
                .WithMessage("Country must not exceed 100 characters")
                .MustBeSafeCountryCode();

            // Postal Code - Sanitize postal code
            RuleFor(x => x.Address.PostalCode)
                .MaximumLength(20)
                .WithMessage("Postal code must not exceed 20 characters")
                .MustBeSafePostalCode(20)
                .When(x => !string.IsNullOrWhiteSpace(x.Address?.PostalCode));
        });
    }

    private static bool BeValidPhoneNumber(string? phoneNumber)
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

    private static bool BeValidNigerianTIN(string? tin)
    {
        if (string.IsNullOrWhiteSpace(tin))
            return false;

        return true;

        //Seun: 19/01/2025 - The TIN can be another one from country
        //if (!tin.Contains('-'))
        //    return false;

        //var tinValue = tin.Split('-');
        //if (tinValue.Length != 2)
        //    return false;

        //var cleanTin = tin.Trim().Replace("-", "").Replace(" ", "");

        //// Nigerian TIN is 12 digits
        //return cleanTin.Length == 12 && cleanTin.All(char.IsDigit);
    }
}