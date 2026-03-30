using FluentValidation;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SubmitExistingInvoicesBulk;

/// <summary>
/// Validator for SubmitExistingInvoicesBulkCommand
/// </summary>
public class SubmitExistingInvoicesBulkCommandValidator : AbstractValidator<SubmitExistingInvoicesBulkCommand>
{
    public SubmitExistingInvoicesBulkCommandValidator()
    {
        RuleFor(x => x.InvoiceIds)
            .NotNull()
            .WithMessage("Invoice ID list cannot be null");

        RuleFor(x => x.InvoiceIds)
            .NotEmpty()
            .WithMessage("At least one invoice ID is required for bulk processing");

        RuleFor(x => x.InvoiceIds.Count)
            .LessThanOrEqualTo(100)
            .WithMessage("Bulk processing is limited to 100 invoices per request. Please split your request into smaller batches.");

        RuleForEach(x => x.InvoiceIds)
            .NotEmpty()
            .WithMessage("Invoice ID cannot be empty");
    }
}
