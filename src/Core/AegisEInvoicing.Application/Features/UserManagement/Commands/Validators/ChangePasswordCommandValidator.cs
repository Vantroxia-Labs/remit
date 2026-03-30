using FluentValidation;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.Validators;

/// <summary>
/// Validator for ChangePasswordCommand with strong password validation
/// </summary>
public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required")
            .MaximumLength(255)
            .WithMessage("Current password must not exceed 255 characters");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
            .MinimumLength(8)
            .WithMessage("New password must be at least 8 characters long")
            .MaximumLength(255)
            .WithMessage("New password must not exceed 255 characters")
            .Must(BeStrongPassword)
            .WithMessage("New password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")
            .Must((command, newPassword) => newPassword != command.CurrentPassword)
            .WithMessage("New password must be different from the current password");

        // Note: ChangePasswordCommand doesn't have ConfirmPassword - this would be handled on the UI side
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