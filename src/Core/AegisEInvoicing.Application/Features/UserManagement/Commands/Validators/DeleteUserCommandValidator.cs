using FluentValidation;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.Validators;

/// <summary>
/// Validator for DeleteUserCommand
/// </summary>
public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("User ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Deletion reason is required")
            .MaximumLength(500)
            .WithMessage("Deletion reason must not exceed 500 characters")
            .Must(BeValidReason)
            .WithMessage("Deletion reason must be professional and appropriate");
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