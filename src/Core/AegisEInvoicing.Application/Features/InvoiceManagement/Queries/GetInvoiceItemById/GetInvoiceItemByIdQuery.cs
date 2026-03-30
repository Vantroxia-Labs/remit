using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceItemById;

public record GetInvoiceItemByIdQuery : IRequest<GetInvoiceItemByIdResult>
{
    public Guid InvoiceItemId { get; init; }
}

public record GetInvoiceItemByIdResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
    public InvoiceItemDto? InvoiceItem { get; init; }
}