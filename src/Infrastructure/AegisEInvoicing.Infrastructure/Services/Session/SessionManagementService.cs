using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AegisEInvoicing.Infrastructure.Services.Session;

/// <summary>
/// Service for managing user sessions with concurrent session limits
/// </summary>
public class SessionManagementService : ISessionManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly SessionManagementSettings _settings;
    private readonly ILogger<SessionManagementService> _logger;

    public SessionManagementService(
        IApplicationDbContext context,
        IOptions<SessionManagementSettings> settings,
        ILogger<SessionManagementService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public int MaxConcurrentSessions => _settings.MaxConcurrentSessions;

    public async Task<bool> CanCreateSessionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_settings.EnforceSessionLimit)
            return true;

        var activeSessionCount = await GetActiveSessionCountAsync(userId, cancellationToken);
        return activeSessionCount < _settings.MaxConcurrentSessions;
    }

    public async Task<int> EnforceSessionLimitAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "EnforceSessionLimitAsync called for user {UserId}. Settings: MaxConcurrentSessions={MaxSessions}, EnforceSessionLimit={Enforce}",
            userId, _settings.MaxConcurrentSessions, _settings.EnforceSessionLimit);

        if (!_settings.EnforceSessionLimit)
        {
            _logger.LogInformation("Session limit enforcement is disabled, skipping");
            return 0;
        }

        // Get ALL active sessions for this user (regardless of last activity time)
        // This ensures we terminate existing sessions when a new login occurs
        var activeSessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderBy(s => s.LastActivityAt)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Found {ActiveSessionCount} active sessions for user {UserId}",
            activeSessions.Count, userId);

        // Calculate how many sessions need to be terminated to make room for new session
        // We add 1 because we're about to create a new session
        var sessionsToTerminate = (activeSessions.Count + 1) - _settings.MaxConcurrentSessions;

        _logger.LogInformation(
            "Sessions to terminate: {SessionsToTerminate} (active={Active} + 1 new - max={Max})",
            sessionsToTerminate, activeSessions.Count, _settings.MaxConcurrentSessions);

        if (sessionsToTerminate <= 0)
        {
            _logger.LogInformation("No sessions need to be terminated");
            return 0;
        }

        // Terminate the oldest sessions (those with earliest LastActivityAt)
        var sessionsToEnd = activeSessions.Take(sessionsToTerminate).ToList();

        foreach (var session in sessionsToEnd)
        {
            session.End("Session limit exceeded - terminated by new login from another device");
            _logger.LogWarning(
                "TERMINATING session {SessionId} for user {UserId} due to concurrent session limit. IP: {IpAddress}, UserAgent: {UserAgent}, StartedAt: {StartedAt}",
                session.Id, userId, session.IpAddress, session.UserAgent, session.StartedAt);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Successfully terminated {Count} sessions for user {UserId} due to concurrent login",
            sessionsToEnd.Count, userId);

        return sessionsToEnd.Count;
    }

    public async Task<int> GetActiveSessionCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var sessionTimeout = TimeSpan.FromMinutes(_settings.SessionTimeoutMinutes);
        var cutoffTime = DateTimeOffset.UtcNow - sessionTimeout;

        return await _context.UserSessions
            .CountAsync(s =>
                s.UserId == userId &&
                s.IsActive &&
                s.LastActivityAt > cutoffTime,
                cancellationToken);
    }

    public async Task<int> TerminateOtherSessionsAsync(
        Guid userId,
        Guid? currentSessionId = null,
        CancellationToken cancellationToken = default)
    {
        var sessionsToEnd = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive && s.Id != currentSessionId)
            .ToListAsync(cancellationToken);

        foreach (var session in sessionsToEnd)
        {
            session.End("Terminated by user request");
        }

        if (sessionsToEnd.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Terminated {Count} sessions for user {UserId} by user request",
                sessionsToEnd.Count, userId);
        }

        return sessionsToEnd.Count;
    }
}
