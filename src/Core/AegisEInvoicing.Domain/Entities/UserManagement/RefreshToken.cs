using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Entities.UserManagement;

/// <summary>
/// Represents a refresh token for JWT authentication
/// Enterprise-level security with token rotation and revocation
/// </summary>
public class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedByIp { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Navigation properties
    public User User { get; private set; } = null!;

    private RefreshToken(
        Guid userId,
        string token,
        DateTimeOffset expiresAt,
        string createdByIp)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedByIp = createdByIp;
    }

    public static RefreshToken Create(
        Guid userId,
        string token,
        DateTimeOffset expiresAt,
        string createdByIp)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required", nameof(token));

        if (string.IsNullOrWhiteSpace(createdByIp))
            throw new ArgumentException("IP address is required", nameof(createdByIp));

        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));

        return new RefreshToken(userId, token, expiresAt, createdByIp);
    }

    public void Revoke(string revokedByIp, string reason, string? replacedByToken = null)
    {
        if (IsRevoked)
            throw new InvalidOperationException("Token is already revoked");

        RevokedAt = DateTimeOffset.UtcNow;
        RevokedByIp = revokedByIp;
        RevokedReason = reason;
        ReplacedByToken = replacedByToken;
    }
}