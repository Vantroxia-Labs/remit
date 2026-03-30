using FluentValidation;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.Validators;

/// <summary>
/// Validator for CreateUserCommand with comprehensive user validation
/// </summary>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
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
            .Must(BeValidBusinessEmail)
            .WithMessage("Email must be a valid business email address");

        //RuleFor(x => x.Password)
        //    .NotEmpty()
        //    .WithMessage("Password is required")
        //    .MinimumLength(8)
        //    .WithMessage("Password must be at least 8 characters long")
        //    .MaximumLength(255)
        //    .WithMessage("Password must not exceed 255 characters")
        //    .Must(BeStrongPassword)
        //    .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50)
            .WithMessage("Phone number must not exceed 50 characters")
            .Must(BeValidPhoneNumber)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Invalid phone number format");

        RuleFor(x => x.RoleIds)
            .NotNull()
            .WithMessage("Role IDs collection is required")
            .Must(x => x.Any())
            .WithMessage("At least one role must be assigned")
            .Must(x => x.Count() <= 10)
            .WithMessage("Cannot assign more than 10 roles to a user")
            .Must(HaveDistinctRoles)
            .WithMessage("Duplicate roles are not allowed");

        RuleForEach(x => x.RoleIds)
            .NotEqual(Guid.Empty)
            .WithMessage("Role ID cannot be empty");
    }

    private static bool BeValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Allow letters, spaces, hyphens, and apostrophes
        return name.All(c => char.IsLetter(c) || c == ' ' || c == '-' || c == '\'');
    }

    private static bool BeValidBusinessEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        // Basic email validation is already done by EmailAddress()
        // Additional business rules can be added here
        var parts = email.Split('@');
        if (parts.Length != 2)
            return false;

        var domain = parts[1].ToLowerInvariant();
        
        // Block obvious personal email domains for business use
        var personalDomains = new[]
        {
            "gmail.com", "yahoo.com", "hotmail.com", "outlook.com",
            "aol.com", "icloud.com", "me.com", "live.com"
        };

        // For business users, we might want to warn but not block personal emails
        // This can be adjusted based on business requirements
        return !personalDomains.Contains(domain) || true; // Currently allows all domains
    }

    private static bool BeStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        var hasUpperCase = password.Any(char.IsUpper);
        var hasLowerCase = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

        return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
    }

    private static bool BeValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return true; // Optional field

        // Remove common formatting characters
        var cleanNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        
        // Must be between 7 and 15 digits (international standard)
        return cleanNumber.All(char.IsDigit) && cleanNumber.Length >= 7 && cleanNumber.Length <= 15;
    }

    private static bool HaveDistinctRoles(IEnumerable<Guid> roleIds)
    {
        return roleIds.Distinct().Count() == roleIds.Count();
    }
}