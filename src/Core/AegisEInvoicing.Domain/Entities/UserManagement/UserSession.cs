using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Entities.UserManagement;

/// <summary>
/// Represents a user session for tracking active logins
/// </summary>
public class UserSession : Entity
{
    public Guid UserId { get; private set; }
    public string IpAddress { get; private set; }
    public string UserAgent { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public DateTimeOffset LastActivityAt { get; private set; }
    public bool IsActive { get; private set; }
    public string? EndReason { get; private set; }
    public string? DeviceInfo { get; private set; }
    public string? Location { get; private set; }

    private UserSession(
        Guid userId,
        string ipAddress,
        string userAgent)
    {
        UserId = userId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        StartedAt = DateTimeOffset.UtcNow;
        LastActivityAt = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public static UserSession Create(
        Guid userId,
        string ipAddress,
        string userAgent,
        string? deviceInfo = null,
        string? location = null)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address is required", nameof(ipAddress));

        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("User agent is required", nameof(userAgent));

        return new UserSession(userId, ipAddress, userAgent)
        {
            DeviceInfo = deviceInfo,
            Location = location
        };
    }

    public void UpdateActivity()
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot update activity on an ended session");

        LastActivityAt = DateTimeOffset.UtcNow;
    }

    public void End(string reason)
    {
        if (!IsActive)
            throw new InvalidOperationException("Session is already ended");

        IsActive = false;
        EndedAt = DateTimeOffset.UtcNow;
        EndReason = reason;
    }

    public TimeSpan GetDuration()
    {
        var endTime = EndedAt ?? DateTimeOffset.UtcNow;
        return endTime - StartedAt;
    }

    public bool IsExpired(TimeSpan sessionTimeout)
    {
        return DateTimeOffset.UtcNow - LastActivityAt > sessionTimeout;
    }

    public void ExpireIfInactive(TimeSpan sessionTimeout)
    {
        if (IsActive && IsExpired(sessionTimeout))
        {
            End("Session timeout");
        }
    }
}