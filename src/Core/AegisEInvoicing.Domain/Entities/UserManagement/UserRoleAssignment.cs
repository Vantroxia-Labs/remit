using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Events.UserManagement;

namespace AegisEInvoicing.Domain.Entities.UserManagement;

/// <summary>
/// Represents the assignment of a platform role to a user within a merchant/branch
/// Merchants and branches can assign Aegis-defined roles but cannot create new role definitions
/// </summary>
public class UserRoleAssignment : Entity
{
    public Guid UserId { get; private set; }
    public Guid PlatformRoleId { get; private set; }
    public Guid AssignedBy { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedBy { get; private set; }
    public string? RevocationReason { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    public PlatformRole PlatformRole { get; private set; } = null!;

    private UserRoleAssignment(
        Guid userId, 
        Guid platformRoleId, 
        Guid assignedBy, 
        DateTimeOffset? expiresAt = null)
    {
        UserId = userId;
        PlatformRoleId = platformRoleId;
        AssignedBy = assignedBy;
        AssignedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        IsActive = true;
    }

    public static UserRoleAssignment Create(
        Guid userId, 
        Guid platformRoleId, 
        Guid assignedBy, 
        DateTimeOffset? expiresAt = null)
    {
        return new UserRoleAssignment(userId, platformRoleId, assignedBy, expiresAt);
    }

    public void Revoke(Guid revokedBy, string? reason = null)
    {
        if (!IsActive)
            throw new InvalidOperationException("Role assignment is already revoked");

        IsActive = false;
        RevokedAt = DateTimeOffset.UtcNow;
        RevokedBy = revokedBy;
        RevocationReason = reason;
    }

    public void Extend(DateTimeOffset newExpirationDate)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot extend a revoked role assignment");

        if (newExpirationDate <= DateTimeOffset.UtcNow)
            throw new ArgumentException("New expiration date must be in the future", nameof(newExpirationDate));

        ExpiresAt = newExpirationDate;
    }

    public bool IsExpired() => ExpiresAt.HasValue ? ExpiresAt <= DateTimeOffset.UtcNow : false;

    public bool IsValid() => IsActive && !IsExpired();

    public bool IsValidAndActive() => IsValid();
}