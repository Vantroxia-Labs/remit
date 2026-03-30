using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateFIRSInvoice;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitBulkInvoice;

/// <summary>
/// Command to create and submit multiple invoices through the complete pipeline
/// </summary>
public record CreateAndSubmitBulkInvoiceCommand : IRequest<CreateAndSubmitBulkInvoiceResult>, ITransactionalCommand
{
    /// <summary>
    /// List of invoices to create and submit
    /// </summary>
    public List<CreateFIRSInvoiceCommand> Invoices { get; init; } = [];

    /// <summary>
    /// Creates a bulk command from a list of invoice data
    /// </summary>
    public static CreateAndSubmitBulkInvoiceCommand FromInvoiceList(List<CreateFIRSInvoiceCommand> invoices)
    {
        return new CreateAndSubmitBulkInvoiceCommand
        {
            Invoices = invoices
        };
    }
}
