using FluentValidation;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoiceItem;

public class CreateInvoiceItemCommandValidator : AbstractValidator<CreateInvoiceItemCommand>
{
    public CreateInvoiceItemCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty()
            .WithMessage("Invoice ID is required");

        RuleFor(x => x.BusinessItemId)
           .NotEmpty()
           .WithMessage("Business Item ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Invoiced quantity must be greater than zero");
    }
}