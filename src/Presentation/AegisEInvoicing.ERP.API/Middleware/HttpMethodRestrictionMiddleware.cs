using Microsoft.Extensions.Primitives;

namespace AegisEInvoicing.ERP.API.Middleware;

/// <summary>
/// Middleware to restrict allowed HTTP methods and prevent information disclosure
/// Addresses VAPT finding: OPTIONS method enabled and method enumeration
/// </summary>
public class HttpMethodRestrictionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpMethodRestrictionMiddleware> _logger;
    private readonly HashSet<string> _allowedMethods;

    // Explicitly blocked methods for security (always blocked regardless of config)
    private static readonly HashSet<string> ExplicitlyBlockedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "OPTIONS",   // VAPT: Prevents method enumeration
        "TRACE",     // VAPT: Prevents XST (Cross-Site Tracing) attacks
        "TRACK",     // Microsoft variant of TRACE
        "CONNECT",   // Prevents tunneling attacks
        "HEAD"       // Can be used for reconnaissance
    };

    public HttpMethodRestrictionMiddleware(
        RequestDelegate next,
        ILogger<HttpMethodRestrictionMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;

        // Get allowed methods from configuration (default: GET, POST, PUT, DELETE, PATCH)
        var allowedMethods = configuration
            .GetSection("Security:AllowedHttpMethods")
            .Get<string[]>() ?? new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };

        // Remove any explicitly blocked methods from allowed list (defense in depth)
        _allowedMethods = new HashSet<string>(
            allowedMethods.Where(m => !ExplicitlyBlockedMethods.Contains(m)), 
            StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation(
            "HTTP Method Restriction initialized. Allowed: [{AllowedMethods}], Blocked: [{BlockedMethods}]",
            string.Join(", ", _allowedMethods),
            string.Join(", ", ExplicitlyBlockedMethods));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;

        // Check explicitly blocked methods first (highest priority)
        if (ExplicitlyBlockedMethods.Contains(method))
        {
            _logger.LogWarning(
                "SECURITY VIOLATION: Blocked dangerous HTTP method {Method} from {IpAddress} to {Path}. " +
                "Method: {Method}, User-Agent: {UserAgent}. " +
                "This indicates reconnaissance or attack attempt (method enumeration, XST attack, etc.)",
                method,
                context.Connection.RemoteIpAddress,
                context.Request.Path,
                method,
                context.Request.Headers.UserAgent.ToString());

            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            context.Response.ContentType = "application/json";

            // Per OWASP: "The Allow header should not be sent, as it would disclose which methods are accepted"
            context.Response.Headers.Remove("Allow");
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            var errorResponse = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.5",
                title = "Method Not Allowed",
                status = 405,
                detail = "The requested HTTP method is not supported by this server.",
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
            return;
        }

        // Block disallowed HTTP methods (OPTIONS, TRACE, HEAD, etc.)
        if (!_allowedMethods.Contains(method))
        {
            _logger.LogWarning(
                "SECURITY: Blocked disallowed HTTP method {Method} from {IpAddress} to {Path}. " +
                "This may indicate reconnaissance activity or attack attempt.",
                method,
                context.Connection.RemoteIpAddress,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            context.Response.ContentType = "application/json";

            // SECURITY: Do NOT add Allow header to prevent method enumeration
            // Addresses VAPT finding: OPTIONS method disclosure
            // Attackers should not be able to discover which methods are allowed

            // Remove any headers that might disclose server information
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");
            context.Response.Headers.Remove("Allow"); // Ensure Allow header is not present

            var errorResponse = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.5",
                title = "Method Not Allowed",
                status = 405,
                detail = "The requested HTTP method is not supported.",
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
            return;
        }

        // Continue to next middleware for allowed methods
        await _next(context);
    }
}

/// <summary>
/// Extension method to register HTTP method restriction middleware
/// </summary>
public static class HttpMethodRestrictionMiddlewareExtensions
{
    public static IApplicationBuilder UseHttpMethodRestriction(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HttpMethodRestrictionMiddleware>();
    }
}
