namespace AegisEInvoicing.ERP.API.Middleware;

/// <summary>
/// Middleware to validate Origin and Referer headers to prevent CSRF attacks
/// For SaaS API with API key authentication
/// </summary>
public class OriginValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OriginValidationMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private readonly HashSet<string> _allowedOrigins;
    private readonly bool _enableStrictValidation;

    public OriginValidationMiddleware(
        RequestDelegate next,
        ILogger<OriginValidationMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;

        // Load allowed origins from configuration
        // Handle both array format and comma-separated string format
        var originsArray = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        var originsString = configuration.GetValue<string>("Cors:AllowedOrigins");

        string[] origins;
        if (originsArray != null && originsArray.Length > 0)
        {
            // Array format from appsettings
            origins = originsArray;
        }
        else if (!string.IsNullOrWhiteSpace(originsString))
        {
            // Comma-separated string format from environment variables
            origins = originsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim())
                .ToArray();
        }
        else
        {
            // No origins configured
            origins = Array.Empty<string>();
        }

        _allowedOrigins = new HashSet<string>(
            origins.Where(o => !string.IsNullOrWhiteSpace(o) && !o.StartsWith("${"))
                .Select(NormalizeOrigin),
            StringComparer.OrdinalIgnoreCase);

        // For SaaS API, we can be more lenient since API keys provide authentication
        // but we still want to log suspicious patterns
        _enableStrictValidation = configuration.GetValue<bool>("Security:EnableStrictOriginValidation", false);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate state-changing requests (POST, PUT, DELETE, PATCH)
        if (IsStateChangingRequest(context.Request.Method))
        {
            if (!ValidateOriginAndReferer(context))
            {
                if (_enableStrictValidation)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Success = false,
                        Message = "Request origin validation failed. This request appears to be a Cross-Site Request Forgery attempt.",
                        ErrorCode = "ORIGIN_VALIDATION_FAILED"
                    });
                    return;
                }
                else
                {
                    // Log but allow (since API key provides authentication)
                    _logger.LogWarning(
                        "Origin validation failed but request allowed due to lenient mode. Path: {Path}, IP: {IpAddress}",
                        context.Request.Path,
                        context.Connection.RemoteIpAddress);
                }
            }
        }

        await _next(context);
    }

    private bool ValidateOriginAndReferer(HttpContext context)
    {
        var request = context.Request;

        // Get Origin header (sent by browsers for CORS requests and POST/PUT/DELETE)
        var origin = request.Headers.Origin.FirstOrDefault();

        // Get Referer header (sent by browsers for navigation requests)
        var referer = request.Headers.Referer.FirstOrDefault();

        // For API requests, we primarily rely on Origin header
        // Referer is a fallback for cases where Origin is not present
        string? requestOrigin = origin;

        if (string.IsNullOrWhiteSpace(requestOrigin) && !string.IsNullOrWhiteSpace(referer))
        {
            // Extract origin from referer URL
            if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            {
                requestOrigin = $"{refererUri.Scheme}://{refererUri.Authority}";
            }
        }

        // If no origin or referer, check if it's an API request
        if (string.IsNullOrWhiteSpace(requestOrigin))
        {
            // For SaaS API (server-to-server), missing origin is acceptable
            // But we log it for monitoring
            _logger.LogInformation(
                "State-changing request to {Path} from {IpAddress} missing Origin and Referer headers (typical for server-to-server API calls)",
                request.Path,
                context.Connection.RemoteIpAddress);

            // Allow requests without origin/referer (common for API integrations)
            return !_enableStrictValidation;
        }

        // Normalize and validate against allowed origins
        var normalizedOrigin = NormalizeOrigin(requestOrigin);

        if (_allowedOrigins.Count > 0 && _allowedOrigins.Contains(normalizedOrigin))
        {
            return true;
        }

        // Check if origin matches the host (same-origin request)
        var requestHost = $"{request.Scheme}://{request.Host}";
        if (normalizedOrigin.Equals(NormalizeOrigin(requestHost), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Log suspicious request
        _logger.LogWarning(
            "Request origin validation warning. Origin: {Origin}, Path: {Path}, IP: {IpAddress}, Configured origins: {AllowedOrigins}",
            requestOrigin,
            request.Path,
            context.Connection.RemoteIpAddress,
            _allowedOrigins.Count > 0 ? string.Join(", ", _allowedOrigins) : "none configured");

        return false;
    }

    private static bool IsStateChangingRequest(string method)
    {
        return method is "POST" or "PUT" or "DELETE" or "PATCH";
    }

    private static string NormalizeOrigin(string origin)
    {
        // Remove trailing slashes and normalize to lowercase
        return origin.TrimEnd('/').ToLowerInvariant();
    }
}

/// <summary>
/// Extension methods for registering origin validation middleware
/// </summary>
public static class OriginValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseOriginValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OriginValidationMiddleware>();
    }
}
