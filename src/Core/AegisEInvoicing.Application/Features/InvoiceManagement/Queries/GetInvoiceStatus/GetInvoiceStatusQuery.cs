using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceStatus;

public record GetInvoiceStatusQuery : IRequest<GetInvoiceStatusResult>
{
    public Guid InvoiceId { get; init; }
    public Guid BusinessId { get; init; }
}