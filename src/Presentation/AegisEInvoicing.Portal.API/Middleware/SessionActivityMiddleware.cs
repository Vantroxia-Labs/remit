using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AegisEInvoicing.Portal.API.Middleware;

/// <summary>
/// Middleware to track user session activity on each authenticated request
/// Prevents concurrent logins by enforcing session limits and timeout
/// </summary>
public class SessionActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionActivityMiddleware> _logger;

    public SessionActivityMiddleware(
        RequestDelegate next,
        ILogger<SessionActivityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IApplicationDbContext dbContext,
        ISessionManagementService sessionManagementService)
    {
        // Only track authenticated requests
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                try
                {
                    await UpdateSessionActivityAsync(context, userId, dbContext, sessionManagementService);
                }
                catch (Exception ex)
                {
                    // Log error but don't block the request
                    _logger.LogError(ex, "Failed to update session activity for user {UserId}", userId);
                }
            }
        }

        // If the middleware already wrote a response (e.g. 401 session expired), stop the pipeline
        if (context.Response.HasStarted)
            return;

        await _next(context);
    }

    private async Task UpdateSessionActivityAsync(
        HttpContext context,
        Guid userId,
        IApplicationDbContext dbContext,
        ISessionManagementService sessionManagementService)
    {
        var ipAddress = GetIpAddress(context);

        // Find the most recent active session for this user and IP
        var session = await dbContext.UserSessions
            .Where(s => s.UserId == userId && s.IsActive && s.IpAddress == ipAddress)
            .OrderByDescending(s => s.LastActivityAt)
            .FirstOrDefaultAsync();

        if (session != null)
        {
            // Check if session has expired
            var sessionTimeout = TimeSpan.FromMinutes(sessionManagementService.MaxConcurrentSessions == 1 ? 15 : 30);

            if (session.IsExpired(sessionTimeout))
            {
                session.End("Session timeout due to inactivity");
                await dbContext.SaveChangesAsync();

                _logger.LogWarning(
                    "Session {SessionId} for user {UserId} expired due to inactivity",
                    session.Id, userId);

                // Return 401 to force re-authentication
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.Append("Session-Expired", "true");
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Session expired",
                    message = "Your session has expired due to inactivity. Please log in again."
                });
                return;
            }

            // Update last activity time
            session.UpdateActivity();
            await dbContext.SaveChangesAsync();

            _logger.LogDebug(
                "Updated session activity for user {UserId}, session {SessionId}",
                userId, session.Id);
        }
        else
        {
            // No active session found - user may have been logged out
            _logger.LogWarning(
                "No active session found for authenticated user {UserId} from IP {IpAddress}",
                userId, ipAddress);

            // Allow the request to proceed but log the anomaly
            // The JWT is still valid, but session was terminated (e.g., due to concurrent login)
        }
    }

    private static string GetIpAddress(HttpContext context)
    {
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            // Take the first IP if there are multiple (proxy chain)
            ipAddress = ipAddress.Split(',')[0].Trim();
        }

        ipAddress ??= context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // Strip port number if present (e.g., "102.88.113.175:50837" -> "102.88.113.175")
        // Handle both IPv4 (ip:port) and IPv6 ([ip]:port) formats
        if (ipAddress.Contains(':') && !ipAddress.StartsWith('['))
        {
            // IPv4 with port - take everything before the last colon
            var lastColonIndex = ipAddress.LastIndexOf(':');
            ipAddress = ipAddress.Substring(0, lastColonIndex);
        }
        else if (ipAddress.StartsWith('[') && ipAddress.Contains("]:"))
        {
            // IPv6 with port - take everything before ]:
            var bracketPortIndex = ipAddress.IndexOf("]:");
            ipAddress = ipAddress.Substring(1, bracketPortIndex - 1);
        }

        return ipAddress;
    }
}

/// <summary>
/// Extension method to register session activity middleware
/// </summary>
public static class SessionActivityMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionActivityTracking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionActivityMiddleware>();
    }
}
