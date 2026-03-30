using FluentValidation;

namespace AegisEInvoicing.Application.Features.PartyManagement.Queries.GetPartiesByBusinessId;

public class GetPartiesByBusinessIdQueryValidator : AbstractValidator<GetPartiesByBusinessIdQuery>
{
    public GetPartiesByBusinessIdQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .WithMessage("Search term must not exceed 200 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField)
            .WithMessage("Sort field must be one of: Name, Email, CreatedAt")
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy));
    }

    private static bool BeValidSortField(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return true;

        var validSortFields = new[] { "name", "email", "createdat" };
        return validSortFields.Contains(sortBy.ToLower());
    }
}