using FluentValidation;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.Validators;

/// <summary>
/// Validator for AssignRoleCommand
/// </summary>
public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("User ID is required");

        RuleFor(x => x.RoleId)
            .NotEqual(Guid.Empty)
            .WithMessage("Role ID is required");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTimeOffset.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiration date must be in the future");

        RuleFor(x => x.ExpiresAt)
            .LessThan(DateTimeOffset.UtcNow.AddYears(5))
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiration date cannot be more than 5 years in the future");
    }
}