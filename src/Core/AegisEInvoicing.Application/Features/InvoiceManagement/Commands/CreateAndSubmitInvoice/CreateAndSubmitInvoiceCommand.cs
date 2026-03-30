using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateFIRSInvoice;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitInvoice;

/// <summary>
/// Command to create an invoice and automatically submit it through the complete pipeline:
/// Create ? Validate ? Sign ? Transmit
/// </summary>
public record CreateAndSubmitInvoiceCommand : IRequest<CreateAndSubmitInvoiceResult>, ITransactionalCommand
{
    /// <summary>
    /// The invoice creation data (same as CreateFIRSInvoiceCommand)
    /// </summary>
    public CreateFIRSInvoiceCommand InvoiceData { get; init; } = null!;

    /// <summary>
    /// Creates a command from the invoice data
    /// </summary>
    public static CreateAndSubmitInvoiceCommand FromInvoiceData(CreateFIRSInvoiceCommand invoiceData)
    {
        return new CreateAndSubmitInvoiceCommand
        {
            InvoiceData = invoiceData
        };
    }
}
