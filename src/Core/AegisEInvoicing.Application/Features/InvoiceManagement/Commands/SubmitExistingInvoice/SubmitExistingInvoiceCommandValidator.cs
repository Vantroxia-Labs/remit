using FluentValidation;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SubmitExistingInvoice;

/// <summary>
/// Validator for SubmitExistingInvoiceCommand
/// </summary>
public class SubmitExistingInvoiceCommandValidator : AbstractValidator<SubmitExistingInvoiceCommand>
{
    public SubmitExistingInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty()
            .WithMessage("Invoice ID is required");
    }
}
