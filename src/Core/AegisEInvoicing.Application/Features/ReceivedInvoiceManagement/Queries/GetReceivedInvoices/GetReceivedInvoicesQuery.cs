using AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Queries.GetReceivedInvoices;

/// <summary>
/// Query to retrieve received invoices with pagination and filtering
/// </summary>
public sealed record GetReceivedInvoicesQuery : IRequest<GetReceivedInvoicesResult>
{
    /// <summary>
    /// Start date filter (optional)
    /// </summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>
    /// End date filter (optional)
    /// </summary>
    public DateOnly? EndDate { get; init; }

    /// <summary>
    /// Search term for supplier name or IRN (optional)
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Sort field (default: IssueDate)
    /// </summary>
    public string SortBy { get; init; } = "IssueDate";

    /// <summary>
    /// Sort direction (asc or desc, default: desc)
    /// </summary>
    public string SortDirection { get; init; } = "desc";
}

/// <summary>
/// Result containing paginated received invoices (lightweight list view)
/// </summary>
public sealed record GetReceivedInvoicesResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<ReceivedInvoiceListDto> Invoices { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
