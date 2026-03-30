using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.UserManagement.DTOs;

/// <summary>
/// DTO for Aegis users with Aegis-specific properties
/// Contains information relevant to platform administrators managing Aegis users
/// </summary>
public record AegisUserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Status,
    bool IsEmailVerified,
    DateTimeOffset? LastLoginAt,
    bool MustChangePassword,
    int FailedLoginAttempts,
    DateTimeOffset? LockedOutUntil,
    AegisRole AegisRole,
    string? AegisEmployeeId,
    string? AegisDepartment,
    DateTimeOffset? LastAegisActivityAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string CreatedByName);

/// <summary>
/// Summary DTO for Aegis users list view
/// Contains essential information for listing and searching Aegis users
/// </summary>
public record AegisUserSummaryDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Status,
    AegisRole AegisRole,
    string AegisRoleDisplayName,
    string? AegisEmployeeId,
    string? AegisDepartment,
    DateTimeOffset? LastLoginAt,
    bool MustChangePassword,
    DateTimeOffset CreatedAt);
