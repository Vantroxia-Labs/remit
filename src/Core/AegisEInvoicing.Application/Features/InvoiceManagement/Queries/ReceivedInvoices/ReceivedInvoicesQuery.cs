using AegisEInvoicing.Application.Common.Models;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.ReceivedInvoices;

public sealed record ReceivedInvoicesQuery : IRequest<PaginatedList<ReceivedInvoicesDto>>
{
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? OrderBy { get; init; }
    public bool OrderByDescending { get; init; } = true;
}
