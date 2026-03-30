using FluentValidation;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitInvoice;

/// <summary>
/// Validator for CreateAndSubmitInvoiceCommand
/// Delegates validation to the underlying CreateFIRSInvoiceCommand validator
/// </summary>
public class CreateAndSubmitInvoiceCommandValidator : AbstractValidator<CreateAndSubmitInvoiceCommand>
{
    public CreateAndSubmitInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceData)
            .NotNull()
            .WithMessage("Invoice data is required");

        // The actual invoice data validation is handled by CreateFIRSInvoiceCommandValidator
        // when the CreateFIRSInvoiceCommand is sent through the mediator
    }
}
