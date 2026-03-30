using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.DeleteInvoiceItem;

public record DeleteInvoiceItemCommand : IRequest<DeleteInvoiceItemResult>
{
    public Guid InvoiceItemId { get; init; }
}