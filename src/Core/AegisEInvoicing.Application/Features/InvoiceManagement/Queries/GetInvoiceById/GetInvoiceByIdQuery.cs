using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceById;

public record GetInvoiceByIdQuery : IRequest<GetInvoiceByIdResult>
{
    public Guid InvoiceId { get; init; }
}

public record GetInvoiceByIdResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
    public InvoiceDetailsDto? Invoice { get; init; }
}