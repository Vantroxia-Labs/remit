using FluentValidation;

namespace AegisEInvoicing.Application.Features.Authentication.Commands.RefreshToken;

/// <summary>
/// Validator for RefreshTokenCommand with security validation
/// </summary>
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required")
            .MaximumLength(500)
            .WithMessage("Refresh token must not exceed 500 characters")
            .Must(BeValidBase64Token)
            .WithMessage("Invalid refresh token format");

        RuleFor(x => x.IpAddress)
            .NotEmpty()
            .WithMessage("IP address is required")
            .MaximumLength(45)
            .WithMessage("IP address must not exceed 45 characters")
            .Must(BeValidIpAddress)
            .WithMessage("Invalid IP address format");
    }

    private static bool BeValidBase64Token(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            Convert.FromBase64String(token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool BeValidIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }
}