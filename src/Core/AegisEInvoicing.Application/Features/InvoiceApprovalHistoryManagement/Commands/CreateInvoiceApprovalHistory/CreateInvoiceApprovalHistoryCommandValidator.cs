using FluentValidation;

namespace AegisEInvoicing.Application.Features.InvoiceApprovalHistoryManagement.Commands.CreateInvoiceApprovalHistory;

public class CreateInvoiceApprovalHistoryCommandValidator : AbstractValidator<CreateInvoiceApprovalHistoryCommand>
{
    public CreateInvoiceApprovalHistoryCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty()
            .WithMessage("Invoice ID is required");

        RuleFor(x => x.InvoiceStatus)
            .IsInEnum()
            .WithMessage("Valid invoice status is required");
    }
}