using FluentValidation;

namespace AegisEInvoicing.Application.Features.LicenseManagement.Commands.ActivateLicense;

/// <summary>
/// Validator for ActivateLicenseCommand
/// </summary>
public class ActivateLicenseCommandValidator : AbstractValidator<ActivateLicenseCommand>
{
    public ActivateLicenseCommandValidator()
    {
        RuleFor(x => x.LicenseKey)
            .NotEmpty()
            .WithMessage("License key is required")
            .MinimumLength(10)
            .WithMessage("License key must be at least 10 characters long");
    }
}
