using FluentValidation;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.Validators;

/// <summary>
/// Validator for ResetPasswordCommand
/// </summary>
public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("User ID is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
            .MinimumLength(8)
            .WithMessage("New password must be at least 8 characters long")
            .MaximumLength(255)
            .WithMessage("New password must not exceed 255 characters")
            .Must(BeStrongPassword)
            .WithMessage("New password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character");
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
}