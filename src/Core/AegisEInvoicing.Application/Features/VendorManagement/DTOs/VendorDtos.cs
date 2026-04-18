using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.VendorManagement.DTOs;

public record VendorGroupDto(
    Guid Id,
    string Name,
    string? Description,
    Guid BusinessId,
    int VendorCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record VendorGroupSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    int VendorCount,
    DateTimeOffset CreatedAt);

public record VendorDto(
    Guid Id,
    string BusinessName,
    string Email,
    string? Phone,
    VendorStatus Status,
    Guid BusinessId,
    Guid VendorGroupId,
    string VendorGroupName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record VendorSummaryDto(
    Guid Id,
    string BusinessName,
    string Email,
    string? Phone,
    VendorStatus Status,
    Guid VendorGroupId,
    string VendorGroupName,
    DateTimeOffset CreatedAt);

// Command Results
public record VendorGroupResult(bool IsSuccess, string Message, Guid? Id = null);
public record VendorResult(bool IsSuccess, string Message, Guid? Id = null);
