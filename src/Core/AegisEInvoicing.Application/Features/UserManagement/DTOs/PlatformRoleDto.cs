namespace AegisEInvoicing.Application.Features.UserManagement.DTOs;

public record PlatformRoleDto(
    Guid Id,
    string Name,
    string Description,
    string Category,
    int SortOrder,
    bool IsSystemRole,
    bool IsActive,
    List<string> Permissions,
    int AssignedUserCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record RoleAssignmentDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserEmail,
    Guid PlatformRoleId,
    string RoleName,
    DateTimeOffset AssignedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt,
    string? RevocationReason,
    bool IsActive,
    bool IsExpired);

public record CreateRoleDto(
    string Name,
    string Description,
    string Category,
    int SortOrder,
    List<string> Permissions,
    bool IsSystemRole = false);

public record UpdateRoleDto(
    string Name,
    string Description,
    string Category,
    int SortOrder,
    List<string> Permissions);