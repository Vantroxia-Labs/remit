using AegisEInvoicing.Application.Common.Interfaces;
using System.Text.Json;

namespace AegisEInvoicing.Portal.API.Middleware;

/// <summary>
/// Middleware that prevents replay attacks by validating request nonces and timestamps.
/// Protects critical invoice operations from being replayed by attackers.
/// </summary>
public sealed class ReplayProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ReplayProtectionMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public ReplayProtectionMiddleware(
        RequestDelegate next,
        ILogger<ReplayProtectionMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task InvokeAsync(HttpContext context, IResponseIntegrityService integrityService)
    {
        // Check if replay protection is enabled
        var isEnabled = _configuration.GetValue<bool>("ResponseIntegrity:EnableReplayProtection", true);

        if (!isEnabled)
        {
            await _next(context);
            return;
        }

        // Only protect critical endpoints
        if (!IsCriticalEndpoint(context))
        {
            await _next(context);
            return;
        }

        try
        {
            // Extract nonce from header
            var nonce = context.Request.Headers["X-Request-Nonce"].FirstOrDefault();

            if (string.IsNullOrEmpty(nonce))
            {
                _logger.LogWarning(
                    "Replay protection: Missing nonce for critical endpoint {Path} from IP {IP}",
                    context.Request.Path,
                    context.Connection.RemoteIpAddress);

                await WriteUnauthorizedResponse(context, "Missing request nonce. Include 'X-Request-Nonce' header.");
                return;
            }

            // Extract and validate timestamp
            var timestampHeader = context.Request.Headers["X-Request-Timestamp"].FirstOrDefault();
            if (string.IsNullOrEmpty(timestampHeader) ||
                !DateTime.TryParse(timestampHeader, out var requestTimestamp))
            {
                _logger.LogWarning(
                    "Replay protection: Invalid timestamp for critical endpoint {Path}",
                    context.Request.Path);

                await WriteUnauthorizedResponse(context, "Missing or invalid request timestamp. Include 'X-Request-Timestamp' header.");
                return;
            }

            // Check if request is too old (default: 5 minutes)
            var maxAgeMinutes = _configuration.GetValue<int>("ResponseIntegrity:MaxRequestAgeMinutes", 5);
            var requestAge = DateTime.UtcNow - requestTimestamp;

            if (requestAge.TotalMinutes > maxAgeMinutes)
            {
                _logger.LogWarning(
                    "Replay protection: Request too old ({Age} minutes) for endpoint {Path}",
                    requestAge.TotalMinutes,
                    context.Request.Path);

                await WriteUnauthorizedResponse(context, $"Request expired. Requests must be made within {maxAgeMinutes} minutes.");
                return;
            }

            // Validate nonce (prevents replay)
            var isNonceValid = await integrityService.ValidateNonceAsync(nonce, maxAgeMinutes);

            if (!isNonceValid)
            {
                _logger.LogWarning(
                    "Replay protection: Invalid or reused nonce detected for endpoint {Path} from IP {IP}. Possible replay attack.",
                    context.Request.Path,
                    context.Connection.RemoteIpAddress);

                await WriteUnauthorizedResponse(context, "Invalid or reused request nonce. Possible replay attack detected.");
                return;
            }

            _logger.LogDebug(
                "Replay protection: Request validated successfully for {Path}",
                context.Request.Path);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in replay protection middleware for endpoint {Path}", context.Request.Path);
            throw;
        }
    }

    private bool IsCriticalEndpoint(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = context.Request.Method.ToUpperInvariant();

        // Only protect POST/PUT/DELETE operations on critical endpoints
        if (method != "POST" && method != "PUT" && method != "DELETE")
        {
            return false;
        }

        // Define critical endpoints that require replay protection
        var criticalPaths = new[]
        {
            "/api/invoice/validate",
            "/api/invoice/sign",
            "/api/invoice/transmit",
            "/api/invoice/approve",
            "/api/invoice/reject",
            "/api/invoice/create",
            "/api/invoice/update",
            "/api/invoice/delete",
            "/api/authentication/login",
            "/api/authentication/refresh",
            "/api/business/create",
            "/api/business/update",
            "/api/business/delete",
            "/api/party/create",
            "/api/party/update",
            "/api/party/delete"
        };

        return criticalPaths.Any(cp => path.Contains(cp));
    }

    private async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new
        {
            Success = false,
            Message = message,
            ErrorCode = "REPLAY_PROTECTION_FAILURE",
            Timestamp = DateTime.UtcNow,
            TraceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Extension methods for registering replay protection middleware
/// </summary>
public static class ReplayProtectionMiddlewareExtensions
{
    public static IApplicationBuilder UseReplayProtection(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ReplayProtectionMiddleware>();
    }
}
