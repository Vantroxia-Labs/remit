using FluentValidation;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.UserManagement.Queries.GetAegisUsers;

/// <summary>
/// Validator for GetAegisUsersQuery to ensure proper pagination and filtering parameters
/// </summary>
public class GetAegisUsersQueryValidator : AbstractValidator<GetAegisUsersQuery>
{
    public GetAegisUsersQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(500)
            .WithMessage("Search term must not exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));

        RuleFor(x => x.AegisRole)
            .IsInEnum()
            .WithMessage("Invalid Aegis role specified")
            .When(x => x.AegisRole.HasValue);

        RuleFor(x => x.AegisDepartment)
            .MaximumLength(100)
            .WithMessage("Aegis Department filter must not exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.AegisDepartment));

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField)
            .WithMessage("Invalid sort field. Valid fields are: FirstName, LastName, Email, CreatedAt, LastLoginAt, AegisRole")
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy));

        RuleFor(x => x.CreatedAfter)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .WithMessage("Created after date cannot be in the future")
            .When(x => x.CreatedAfter.HasValue);

        RuleFor(x => x.CreatedBefore)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .WithMessage("Created before date cannot be in the future")
            .When(x => x.CreatedBefore.HasValue);

        RuleFor(x => x)
            .Must(x => !x.CreatedAfter.HasValue || !x.CreatedBefore.HasValue || x.CreatedAfter.Value <= x.CreatedBefore.Value)
            .WithMessage("Created after date must be before or equal to created before date")
            .When(x => x.CreatedAfter.HasValue && x.CreatedBefore.HasValue);

        RuleFor(x => x.LastLoginAfter)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .WithMessage("Last login after date cannot be in the future")
            .When(x => x.LastLoginAfter.HasValue);

        RuleFor(x => x.LastLoginBefore)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .WithMessage("Last login before date cannot be in the future")
            .When(x => x.LastLoginBefore.HasValue);

        RuleFor(x => x)
            .Must(x => !x.LastLoginAfter.HasValue || !x.LastLoginBefore.HasValue || x.LastLoginAfter.Value <= x.LastLoginBefore.Value)
            .WithMessage("Last login after date must be before or equal to last login before date")
            .When(x => x.LastLoginAfter.HasValue && x.LastLoginBefore.HasValue);
    }

    private static bool BeValidSortField(string? sortField)
    {
        if (string.IsNullOrWhiteSpace(sortField))
            return true; // Default will be applied

        var validSortFields = new[]
        {
            "FirstName", "LastName", "Email", "CreatedAt", "LastLoginAt", "AegisRole",
            "firstname", "lastname", "email", "createdat", "lastloginat", "Aegisrole"
        };

        return validSortFields.Contains(sortField, StringComparer.OrdinalIgnoreCase);
    }
}
