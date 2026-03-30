using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceItemsByInvoiceId;

public record GetInvoiceItemsByInvoiceIdQuery : IRequest<GetInvoiceItemsByInvoiceIdResult>
{
    public Guid InvoiceId { get; init; }
}

public record GetInvoiceItemsByInvoiceIdResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
    public List<InvoiceItemDto> InvoiceItems { get; init; } = [];
}