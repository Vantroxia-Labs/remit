using FluentValidation;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.Validators;

/// <summary>
/// Validator for DeactivateUserCommand
/// </summary>
public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("User ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Deactivation reason is required")
            .MaximumLength(500)
            .WithMessage("Deactivation reason must not exceed 500 characters")
            .Must(BeValidReason)
            .WithMessage("Deactivation reason must be professional and appropriate");
    }

    private static bool BeValidReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return false;

        // Basic profanity and inappropriate content check
        var inappropriateTerms = new[]
        {
            "hate", "stupid", "idiot", "moron", "dumb"
        };

        var lowerReason = reason.ToLowerInvariant();
        return !inappropriateTerms.Any(term => lowerReason.Contains(term));
    }
}