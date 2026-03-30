using Microsoft.Extensions.Primitives;

namespace AegisEInvoicing.Portal.API.Middleware;

/// <summary>
/// Middleware to enforce HTTPS connections and block unencrypted HTTP requests
/// Addresses VAPT finding: Unencrypted communications
/// Protects against: Man-in-the-middle attacks, credential interception, data exposure
/// </summary>
public class HttpsEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpsEnforcementMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public HttpsEnforcementMiddleware(
        RequestDelegate next,
        ILogger<HttpsEnforcementMiddleware> logger,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // =================================================================
        // HTTPS ENFORCEMENT CONFIGURATION
        // =================================================================

        // Check if HTTPS enforcement is enabled (default: true in production, false in development)
        var enforceHttps = _configuration.GetValue<bool>(
            "Security:EnforceHttps",
            _environment.IsProduction());

        // Check if request is over HTTPS
        var isHttps = context.Request.IsHttps;

        // =================================================================
        // ENFORCE HTTPS FOR ALL REQUESTS
        // =================================================================

        if (enforceHttps && !isHttps)
        {
            // Get the HTTPS port from configuration (default: 443)
            var httpsPort = _configuration.GetValue<int>("Security:HttpsPort", 443);

            // Build HTTPS redirect URL
            var host = context.Request.Host;

            // Remove HTTP port if present and add HTTPS port if not default
            var httpsHost = httpsPort == 443
                ? new HostString(host.Host)
                : new HostString(host.Host, httpsPort);

            var httpsUrl = $"https://{httpsHost}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";

            _logger.LogWarning(
                "HTTPS enforcement: Blocking unencrypted HTTP request from {IpAddress} to {Path}. " +
                "Redirecting to: {HttpsUrl}",
                context.Connection.RemoteIpAddress,
                context.Request.Path,
                httpsUrl);

            // Check if we should redirect or block
            var blockHttp = _configuration.GetValue<bool>("Security:BlockHttpRequests", false);

            if (blockHttp)
            {
                // Strict mode: Return 403 Forbidden for HTTP requests
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                // Remove any headers that might disclose server information
                context.Response.Headers.Remove("Server");
                context.Response.Headers.Remove("X-Powered-By");

                var errorResponse = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    title = "HTTPS Required",
                    status = 403,
                    detail = "This API requires HTTPS for all requests. Unencrypted HTTP connections are not permitted. " +
                             "Please use HTTPS to ensure your credentials and data are transmitted securely.",
                    traceId = context.TraceIdentifier
                };

                await context.Response.WriteAsJsonAsync(errorResponse);
                return;
            }
            else
            {
                // Redirect mode: Permanently redirect HTTP to HTTPS (301)
                context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
                context.Response.Headers.Location = httpsUrl;

                // Add HSTS header to ensure future requests use HTTPS
                var hstsMaxAge = _configuration.GetValue<int>("SecurityHeaders:HSTS:MaxAge", 31536000);
                context.Response.Headers["Strict-Transport-Security"] = $"max-age={hstsMaxAge}; includeSubDomains; preload";

                return;
            }
        }

        // =================================================================
        // LOG HTTPS USAGE
        // =================================================================

        if (isHttps)
        {
            _logger.LogDebug(
                "Secure HTTPS request from {IpAddress} to {Path}",
                context.Connection.RemoteIpAddress,
                context.Request.Path);
        }

        // =================================================================
        // SECURITY WARNINGS FOR SENSITIVE ENDPOINTS
        // =================================================================

        // Warn if sensitive endpoints are accessed over HTTP (even if not enforcing)
        if (!isHttps && IsSensitiveEndpoint(context.Request.Path))
        {
            _logger.LogError(
                "SECURITY RISK: Sensitive endpoint {Path} accessed over unencrypted HTTP from {IpAddress}. " +
                "Credentials and personal data may be exposed!",
                context.Request.Path,
                context.Connection.RemoteIpAddress);
        }

        await _next(context);
    }

    /// <summary>
    /// Determines if the endpoint handles sensitive data (login, passwords, personal info)
    /// </summary>
    private bool IsSensitiveEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

        // List of sensitive endpoint patterns
        var sensitivePatterns = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/change-password",
            "/api/auth/reset-password",
            "/api/auth/forgot-password",
            "/api/user/profile",
            "/api/business/create",
            "/api/business/update",
            "/api/invoice/create",
            "/api/payment",
            "/api/settings",
        };

        return sensitivePatterns.Any(pattern => pathValue.Contains(pattern));
    }
}

/// <summary>
/// Extension methods for registering HTTPS enforcement middleware
/// </summary>
public static class HttpsEnforcementMiddlewareExtensions
{
    public static IApplicationBuilder UseHttpsEnforcement(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HttpsEnforcementMiddleware>();
    }
}
