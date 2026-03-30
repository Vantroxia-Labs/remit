using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceByIRN;

public record GetInvoiceByIRNQuery : IRequest<GetInvoiceByIRNResult>
{
    public string IRN { get; init; } = null!;
    public Guid BusinessId { get; init; }
}

public record GetInvoiceByIRNResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
    public InvoiceDetailsDto? Invoice { get; init; }
}