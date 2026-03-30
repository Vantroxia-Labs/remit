using FluentValidation;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitBulkInvoice;

/// <summary>
/// Validator for CreateAndSubmitBulkInvoiceCommand
/// </summary>
public class CreateAndSubmitBulkInvoiceCommandValidator : AbstractValidator<CreateAndSubmitBulkInvoiceCommand>
{
    public CreateAndSubmitBulkInvoiceCommandValidator()
    {
        RuleFor(x => x.Invoices)
            .NotNull()
            .WithMessage("Invoice list cannot be null");

        RuleFor(x => x.Invoices)
            .NotEmpty()
            .WithMessage("At least one invoice is required for bulk processing");

        RuleFor(x => x.Invoices.Count)
            .LessThanOrEqualTo(100)
            .WithMessage("Bulk processing is limited to 100 invoices per request. Please split your request into smaller batches.");

        // Individual invoice validation is handled by CreateFIRSInvoiceCommandValidator
        // when each invoice is processed through the pipeline
    }
}
