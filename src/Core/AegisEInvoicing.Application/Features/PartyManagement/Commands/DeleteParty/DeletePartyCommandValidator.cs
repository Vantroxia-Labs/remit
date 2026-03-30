using FluentValidation;

namespace AegisEInvoicing.Application.Features.PartyManagement.Commands.DeleteParty;

public class DeletePartyCommandValidator : AbstractValidator<DeletePartyCommand>
{
    public DeletePartyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Party ID is required");
    }
}