using FluentValidation;

namespace AegisEInvoicing.Application.Features.Authentication.Commands.Logout;

/// <summary>
/// Validator for LogoutCommand
/// </summary>
public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required")
            .MaximumLength(500)
            .WithMessage("Refresh token must not exceed 500 characters");

        RuleFor(x => x.IpAddress)
            .NotEmpty()
            .WithMessage("IP address is required")
            .MaximumLength(45)
            .WithMessage("IP address must not exceed 45 characters")
            .Must(BeValidIpAddress)
            .WithMessage("Invalid IP address format");
    }

    private static bool BeValidIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }
}