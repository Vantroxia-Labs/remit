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
    ItemType ItemType,
    CreateServiceCodeDto ServiceCode,
    Guid ItemCategoryId,
    string ItemDescription,
    decimal UnitPrice,
    Guid BusinessId);

public record UpdateBusinessItemDto(
    string Name,
    ItemType ItemType,
    UpdateServiceCodeDto ServiceCode,
    Guid ItemCategoryId,
    string ItemDescription,
    decimal UnitPrice);

public record BusinessItemSummaryDto(
    Guid Id,
    string ItemId,
    string Name,
    ItemType ItemType,
    string ServiceCode,
    string ServiceCodeName,
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