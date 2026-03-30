using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Infrastructure.Services.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AegisEInvoicing.Portal.API.BackgroundServices;

/// <summary>
/// Background service that periodically cleans up expired and inactive sessions
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    public SessionCleanupService(
        IServiceProvider serviceProvider,
        ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("Session cleanup service stopped");
    }

    private async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<SessionManagementSettings>>().Value;

        var sessionTimeout = TimeSpan.FromMinutes(settings.SessionTimeoutMinutes);
        var cutoffTime = DateTimeOffset.UtcNow - sessionTimeout;

        // Find all active sessions that have exceeded the timeout
        var expiredSessions = await dbContext.UserSessions
            .Where(s => s.IsActive && s.LastActivityAt < cutoffTime)
            .ToListAsync(cancellationToken);

        if (expiredSessions.Count > 0)
        {
            _logger.LogInformation(
                "Found {Count} expired sessions to clean up (timeout: {Timeout} minutes)",
                expiredSessions.Count, settings.SessionTimeoutMinutes);

            foreach (var session in expiredSessions)
            {
                session.End("Session timeout - automatic cleanup");
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully cleaned up {Count} expired sessions",
                expiredSessions.Count);
        }
        else
        {
            _logger.LogDebug("No expired sessions found during cleanup check");
        }

        // Also clean up old refresh tokens (older than 30 days)
        // Note: Use RevokedAt != null instead of IsRevoked (computed property can't be translated to SQL)
        var oldTokenCutoff = DateTimeOffset.UtcNow.AddDays(-30);
        var now = DateTimeOffset.UtcNow;
        var oldTokens = await dbContext.RefreshTokens
            .Where(t => (t.RevokedAt != null || t.ExpiresAt < now) && t.CreatedAt < oldTokenCutoff)
            .ToListAsync(cancellationToken);

        if (oldTokens.Count > 0)
        {
            dbContext.RefreshTokens.RemoveRange(oldTokens);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cleaned up {Count} old refresh tokens",
                oldTokens.Count);
        }
    }
}
