using AegisEInvoicing.Domain.Enums;

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
    ItemType ItemType,
    ServiceCodeDto ServiceCode,
    IReadOnlyCollection<BusinessItemTaxCategoryDto> TaxCategories,
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
    ItemType ItemType,
    CreateServiceCodeDto ServiceCode,
    IEnumerable<CreateBusinessItemTaxCategoryDto> TaxCategories,
    string ItemDescription,
    decimal UnitPrice,
    Guid BusinessId);

public record UpdateBusinessItemDto(
    string Name,
    ItemType ItemType,
    UpdateServiceCodeDto ServiceCode,
    IEnumerable<UpdateBusinessItemTaxCategoryDto> TaxCategories,
    string ItemDescription,
    decimal UnitPrice);

public record BusinessItemSummaryDto(
    Guid Id,
    string ItemId,
    string Name,
    ItemType ItemType,
    string ServiceCode,
    string ServiceCodeName,
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

public record BusinessItemTaxCategoryDto(
    string Code,
    string Name,
    bool IsPercentage,
    decimal? Percent,
    decimal? FlatAmount);

public record CreateBusinessItemTaxCategoryDto(
    string Code,
    string Name,
    bool IsPercentage,
    decimal? Percent,
    decimal? FlatAmount);

public record UpdateBusinessItemTaxCategoryDto(
    string Code,
    string Name,
    bool IsPercentage,
    decimal? Percent,
    decimal? FlatAmount);