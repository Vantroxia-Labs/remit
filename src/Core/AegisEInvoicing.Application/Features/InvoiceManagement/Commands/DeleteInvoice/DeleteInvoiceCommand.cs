using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.DeleteInvoice;

public record DeleteInvoiceCommand : IRequest<DeleteInvoiceResult>
{
    public Guid InvoiceId { get; init; }
}