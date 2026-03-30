namespace AegisEInvoicing.Application.Features.ItemCategoryManagement.DTOs;

public record ItemCategoryDto(
    Guid Id,
    string Name,
    string Description,
    DateTimeOffset CreatedAt);

public record ItemCategoryResult(
    bool IsSuccess,
    string Message,
    Guid? ItemCategoryId = null);

public record GetItemCategoryByIdResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
    public ItemCategoryDto? ItemCategory { get; init; }
}

public record ItemCategorySummaryDto(
    Guid Id,
    string Name,
    string Description,
    DateTimeOffset CreatedAt);