using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetAllInvoicesForBusiness;

public record GetAllInvoicesForBusinessQuery : IRequest<GetAllInvoicesForBusinessResult>
{
    public InvoiceStatus? InvoiceStatus { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? OrderBy { get; init; } = "CreatedAt";
    public bool OrderByDescending { get; init; } = true;
    public Guid BusinessId { get; init; }
}

public record GetAllInvoicesForBusinessResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
    public PaginatedList<InvoiceDto>? Invoices { get; init; }
}