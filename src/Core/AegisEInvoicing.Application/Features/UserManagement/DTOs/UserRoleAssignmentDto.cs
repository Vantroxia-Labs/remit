namespace AegisEInvoicing.Application.Features.UserManagement.DTOs;

public record UserRoleAssignmentDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = default!;
    public string UserFullName { get; init; } = default!;
    public Guid PlatformRoleId { get; init; }
    public string RoleName { get; init; } = default!;
    public string RoleDescription { get; init; } = default!;
    public Guid AssignedBy { get; init; }
    public string AssignedByName { get; init; } = default!;
    public DateTimeOffset AssignedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? RevokedAt { get; init; }
    public Guid? RevokedBy { get; init; }
    public string? RevokedByName { get; init; }
    public string? RevocationReason { get; init; }
    public bool IsExpired { get; init; }
    public bool IsValid { get; init; }
}
