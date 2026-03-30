using FluentValidation;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.Validators;

/// <summary>
/// Validator for RevokeRoleCommand
/// </summary>
public class RevokeRoleCommandValidator : AbstractValidator<RevokeRoleCommand>
{
    public RevokeRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("User ID is required");

        RuleFor(x => x.RoleId)
            .NotEqual(Guid.Empty)
            .WithMessage("Role ID is required");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Revocation reason must not exceed 500 characters")
            .Must(BeValidReason)
            .When(x => !string.IsNullOrWhiteSpace(x.Reason))
            .WithMessage("Revocation reason must be professional and appropriate");
    }

    private static bool BeValidReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return true; // Optional field

        // Basic profanity and inappropriate content check
        var inappropriateTerms = new[]
        {
            "hate", "stupid", "idiot", "moron", "dumb"
        };

        var lowerReason = reason.ToLowerInvariant();
        return !inappropriateTerms.Any(term => lowerReason.Contains(term));
    }
}