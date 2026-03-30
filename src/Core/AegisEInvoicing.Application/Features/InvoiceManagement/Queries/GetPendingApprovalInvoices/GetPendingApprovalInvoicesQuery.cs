using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetPendingApprovalInvoices;

/// <summary>
/// Query to get all invoices pending ClientAdmin approval (ClientAdmin only)
/// </summary>
public record GetPendingApprovalInvoicesQuery : IRequest<PaginatedList<InvoiceDto>>
{
    public string? SearchTerm { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? OrderBy { get; init; }
    public bool OrderByDescending { get; init; } = true;
}
