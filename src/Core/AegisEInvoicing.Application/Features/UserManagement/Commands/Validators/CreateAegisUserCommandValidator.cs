using FluentValidation;
using AegisEInvoicing.Application.Features.UserManagement.Commands.AegisUserCommands;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.Validators;

/// <summary>
/// Validator for CreateAegisUserCommand with comprehensive Aegis user validation
/// Ensures proper validation for platform-level Aegis user creation
/// </summary>
public class CreateAegisUserCommandValidator : AbstractValidator<CreateAegisUserCommand>
{
    public CreateAegisUserCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .MaximumLength(100)
            .WithMessage("First name must not exceed 100 characters")
            .Must(BeValidName)
            .WithMessage("First name can only contain letters, spaces, hyphens, and apostrophes");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .MaximumLength(100)
            .WithMessage("Last name must not exceed 100 characters")
            .Must(BeValidName)
            .WithMessage("Last name can only contain letters, spaces, hyphens, and apostrophes");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email address is required")
            .MaximumLength(255)
            .WithMessage("Email address must not exceed 255 characters")
            .EmailAddress()
            .WithMessage("Invalid email address format")
            .Must(BeValidAegisEmail)
            .WithMessage("Email must be a valid Aegis corporate email address");

        //RuleFor(x => x.Password)
        //    .NotEmpty()
        //    .WithMessage("Password is required")
        //    .MinimumLength(12)
        //    .WithMessage("Aegis user password must be at least 12 characters long")
        //    .MaximumLength(255)
        //    .WithMessage("Password must not exceed 255 characters")
        //    .Must(BeStrongAegisPassword)
        //    .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit, one special character, and cannot contain common patterns");

        RuleFor(x => x.AegisRole)
            .IsInEnum()
            .WithMessage("Invalid Aegis role specified")
            .Must(BeValidAegisRole)
            .WithMessage("Invalid or unrecognized Aegis role");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50)
            .WithMessage("Phone number must not exceed 50 characters")
            .Must(BeValidPhoneNumber)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Invalid phone number format");

        RuleFor(x => x.AegisEmployeeId)
            .MaximumLength(50)
            .WithMessage("Aegis Employee ID must not exceed 50 characters")
            .Must(BeValidEmployeeId)
            .When(x => !string.IsNullOrWhiteSpace(x.AegisEmployeeId))
            .WithMessage("Aegis Employee ID must contain only alphanumeric characters and hyphens");

        RuleFor(x => x.AegisDepartment)
            .MaximumLength(100)
            .WithMessage("Aegis Department must not exceed 100 characters")
            .Must(BeValidDepartmentName)
            .When(x => !string.IsNullOrWhiteSpace(x.AegisDepartment))
            .WithMessage("Department name can only contain letters, numbers, spaces, hyphens, and periods");
    }

    private static bool BeValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Allow letters, spaces, hyphens, and apostrophes
        return name.All(c => char.IsLetter(c) || c == ' ' || c == '-' || c == '\'');
    }

    private static bool BeValidAegisEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        // Basic email validation is already done by EmailAddress()
        var parts = email.Split('@');
        if (parts.Length != 2)
            return false;

        var domain = parts[1].ToLowerInvariant();

        // For Aegis users, enforce corporate email domains
        var acceptedAegisDomains = new[]
        {
            "aegis.com", "ng.aegis.com", "aegis.co.uk", "aegis.ca", "aegis.com.au", "aegis.de",
            "aegis.nl", "aegis.fr", "aegis.it", "aegis.es", "aegis.ch",
            "aegis.ie", "aegis.be", "aegis.at", "aegis.dk", "aegis.se",
            "aegis.no", "aegis.fi", "aegis.pl", "aegis.cz", "aegis.hu",
            "aegis.co.za", "aegis.com.sg", "aegis.co.jp", "aegis.co.kr",
            "aegis.com.cn", "aegis.co.in", "aegis.com.br", "aegis.com.ar",
            "aegis.com.mx", "aegis.cl", "aegis.co.nz",
            "aegisnrs.com", "ng.aegisnrs.com"
        };

        return acceptedAegisDomains.Contains(domain);
    }

    private static bool BeStrongAegisPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        var hasUpperCase = password.Any(char.IsUpper);
        var hasLowerCase = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

        // Additional security for Aegis passwords
        var hasNoCommonPatterns = !ContainsCommonPatterns(password);

        return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar && hasNoCommonPatterns;
    }

    private static bool ContainsCommonPatterns(string password)
    {
        var commonPatterns = new[]
        {
            "password", "123456", "qwerty", "admin", "Aegis",
            "Password", "Password1", "password1", "Password123"
        };

        return commonPatterns.Any(pattern => password.ToLowerInvariant().Contains(pattern.ToLowerInvariant()));
    }

    private static bool BeValidPhoneNumber(string phoneNumber)
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

    private static bool BeValidEmployeeId(string employeeId)
    {
        if (string.IsNullOrWhiteSpace(employeeId))
            return true; // Optional field

        // Allow alphanumeric characters and hyphens only
        return employeeId.All(c => char.IsLetterOrDigit(c) || c == '-');
    }

    private static bool BeValidDepartmentName(string department)
    {
        if (string.IsNullOrWhiteSpace(department))
            return true; // Optional field

        // Allow letters, numbers, spaces, hyphens, and periods
        return department.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '.');
    }

    private static bool BeValidAegisRole(AegisRole role)
    {
        // Ensure the role is one of the defined enum values
        return Enum.IsDefined(typeof(AegisRole), role);
    }
}
