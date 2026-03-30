using FluentValidation;

namespace AegisEInvoicing.Application.Features.Authentication.Commands.Login;

/// <summary>
/// Validator for LoginCommand with comprehensive security validation
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email address is required")
            .MaximumLength(255)
            .WithMessage("Email address must not exceed 255 characters")
            .EmailAddress()
            .WithMessage("Invalid email address format")
            .Must(BeValidEmailDomain)
            .WithMessage("Email domain is not allowed");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long")
            .MaximumLength(255)
            .WithMessage("Password must not exceed 255 characters");

        RuleFor(x => x.IpAddress)
            .NotEmpty()
            .WithMessage("IP address is required");

        RuleFor(x => x.UserAgent)
            .NotEmpty()
            .WithMessage("User agent is required")
            .MaximumLength(1000)
            .WithMessage("User agent must not exceed 1000 characters");
    }

    private static bool BeValidEmailDomain(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var parts = email.Split('@');
        if (parts.Length != 2)
            return false;

        var domain = parts[1].ToLowerInvariant();
        
        // Block common disposable email domains for security
        var blockedDomains = new[]
        {
            "10minutemail.com",
            "guerrillamail.com",
            "mailinator.com",
            "tempmail.org"
        };

        return !blockedDomains.Contains(domain);
    }

    private static bool BeValidIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }
}