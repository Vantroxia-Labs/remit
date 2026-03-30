namespace AegisEInvoicing.Application.Common.Models;

/// <summary>
/// Represents a paginated result
/// </summary>
public sealed class PaginatedResult<T>
{
    public PaginatedResult(
        IEnumerable<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        Items = items.ToList();
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        HasPreviousPage = pageNumber > 1;
        HasNextPage = pageNumber < TotalPages;
    }

    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage { get; }
    public bool HasNextPage { get; }

    public static PaginatedResult<T> Empty(int pageNumber = 1, int pageSize = 10)
    {
        return new PaginatedResult<T>(Enumerable.Empty<T>(), 0, pageNumber, pageSize);
    }
}