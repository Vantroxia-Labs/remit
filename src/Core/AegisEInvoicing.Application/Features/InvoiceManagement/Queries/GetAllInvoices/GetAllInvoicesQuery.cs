using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetAllInvoices;

public record GetAllInvoicesQuery : IRequest<PaginatedList<InvoiceDto>>
{
    public Guid? BusinessId { get; init; }
    public InvoiceStatus? InvoiceStatus { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? SearchTerm { get; init; }
    public AppEnvironmentMode? EnvironmentMode { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? OrderBy { get; init; }
    public bool OrderByDescending { get; init; } = true;
}