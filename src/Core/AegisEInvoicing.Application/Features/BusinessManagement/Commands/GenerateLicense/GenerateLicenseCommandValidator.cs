using FluentValidation;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.GenerateLicense;

public class GenerateLicenseCommandValidator : AbstractValidator<GenerateLicenseCommand>
{
    public GenerateLicenseCommandValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty()
            .WithMessage("Business ID is required");

        RuleFor(x => x.ExpiryDate)
            .NotEmpty()
            .WithMessage("Expiry date is required")
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Expiry date must be in the future")
            .LessThan(DateTime.UtcNow.AddYears(10))
            .WithMessage("Expiry date cannot be more than 10 years in the future");
    }
}
