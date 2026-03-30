using FluentValidation;

namespace AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.Commands.AssignFIRSConfiguration;

public class AssignFIRSConfigurationCommandValidator : AbstractValidator<AssignFIRSConfigurationCommand>
{
    public AssignFIRSConfigurationCommandValidator()
    {
        RuleFor(x => x.FIRSApiConfigurationId)
            .NotEmpty().WithMessage("FIRS API Configuration ID is required")
            .NotEqual(Guid.Empty).WithMessage("FIRS API Configuration ID cannot be empty");
    }
}