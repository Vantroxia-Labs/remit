namespace AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;

public record BusinessItemResult(
bool IsSuccess,
string Message,
Guid? BusinessItemId = null);

public record BusinessItemByIdResult(
bool IsSuccess,
string Message,
BusinessItemDto? BusinessItem = null);


public record BusinessItemDto(
    Guid Id,
    string ItemId,
    string Name,
    ServiceCodeDto ServiceCode,
    TaxCategoryDto TaxCategory,
    Guid ItemCategoryId,
    string? ItemCategoryName,
    string ItemDescription,
    decimal UnitPrice,
    Guid BusinessId,
    string? BusinessName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    Guid CreatedBy,
    Guid? UpdatedBy);

public record CreateBusinessItemDto(
    string Name,
    CreateServiceCodeDto ServiceCode,
    CreateTaxCategoryDto TaxCategory,
    Guid ItemCategoryId,
    string ItemDescription,
    decimal UnitPrice,
    Guid BusinessId);

public record UpdateBusinessItemDto(
    string Name,
    UpdateServiceCodeDto ServiceCode,
    UpdateTaxCategoryDto TaxCategory,
    Guid ItemCategoryId,
    string ItemDescription,
    decimal UnitPrice);

public record BusinessItemSummaryDto(
    Guid Id,
    string ItemId,
    string Name,
    string ServiceCode,
    string ServiceCodeName,
    string TaxCategoryName,
    string ItemCategoryName,
    decimal UnitPrice,
    string BusinessName,
    DateTimeOffset CreatedAt);

public record ServiceCodeDto(
    string Code,
    string Name);

public record CreateServiceCodeDto(
    string Code,
    string Name);

public record UpdateServiceCodeDto(
    string Code,
    string Name);

public record TaxCategoryDto(
    string Name,
    decimal Percent);

public record CreateTaxCategoryDto(
    string Name,
    decimal Percent);

public record UpdateTaxCategoryDto(
    string Name,
    decimal Percent);