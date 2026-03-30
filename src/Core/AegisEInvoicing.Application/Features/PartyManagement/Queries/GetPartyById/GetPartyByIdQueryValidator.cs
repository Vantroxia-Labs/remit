using FluentValidation;

namespace AegisEInvoicing.Application.Features.PartyManagement.Queries.GetPartyById;

public class GetPartyByIdQueryValidator : AbstractValidator<GetPartyByIdQuery>
{
    public GetPartyByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Party ID is required");
    }
}