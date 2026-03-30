namespace AegisEInvoicing.Portal.API.Middleware;

/// <summary>
/// Middleware to validate Origin and Referer headers to prevent CSRF attacks
/// Updated to handle proxy scenarios with X-Forwarded-* headers
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
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();

        _allowedOrigins = new HashSet<string>(
            origins.Select(NormalizeOrigin),
            StringComparer.OrdinalIgnoreCase);

        // Check if strict validation is enabled (default: true)
        _enableStrictValidation = configuration.GetValue<bool>("Security:EnableStrictOriginValidation", true);
        
        _logger.LogInformation(
            "OriginValidationMiddleware initialized. Strict validation: {Enabled}, Allowed origins count: {Count}",
            _enableStrictValidation,
            _allowedOrigins.Count);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate state-changing requests (POST, PUT, DELETE, PATCH)
        if (IsStateChangingRequest(context.Request.Method))
        {
            // Skip validation for authentication endpoints (they use credentials directly)
            if (!IsAuthenticationEndpoint(context.Request.Path))
            {
                // Check if strict validation is enabled
                if (!_enableStrictValidation)
                {
                    // Log but allow through when strict validation is disabled
                    await _next(context);
                    return;
                }

                if (!ValidateOriginAndReferer(context))
                {
                    // Log detailed diagnostic information
                    _logger.LogWarning(
                        "Origin validation FAILED - Method: {Method}, Path: {Path}, " +
                        "Origin: {Origin}, Referer: {Referer}, " +
                        "X-Forwarded-Host: {ForwardedHost}, X-Forwarded-Proto: {ForwardedProto}, " +
                        "Host: {Host}, RemoteIP: {RemoteIP}",
                        context.Request.Method,
                        context.Request.Path,
                        context.Request.Headers.Origin.FirstOrDefault() ?? "null",
                        context.Request.Headers.Referer.FirstOrDefault() ?? "null",
                        context.Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? "null",
                        context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? "null",
                        context.Request.Host.ToString(),
                        context.Connection.RemoteIpAddress?.ToString() ?? "null");

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Success = false,
                        Message = "Request origin validation failed. This request appears to be a Cross-Site Request Forgery attempt.",
                        ErrorCode = "ORIGIN_VALIDATION_FAILED"
                    });
                    return;
                }
            }
        }

        await _next(context);
    }

    private bool ValidateOriginAndReferer(HttpContext context)
    {
        var request = context.Request;

        // This is the most important check for proxy scenarios
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp != null && IsFromTrustedNetwork(remoteIp))
        {
            _logger.LogInformation(
                "Origin validation PASSED - Request from trusted network IP: {IpAddress} to {Path}",
                remoteIp,
                request.Path);
            return true;
        }
        
        // Get Origin header (sent by browsers for CORS requests and POST/PUT/DELETE)
        var origin = request.Headers.Origin.FirstOrDefault();

        // Get Referer header (sent by browsers for navigation requests)
        var referer = request.Headers.Referer.FirstOrDefault();

        // Check for forwarded headers from proxy
        var forwardedHost = request.Headers["X-Forwarded-Host"].FirstOrDefault();
        var forwardedProto = request.Headers["X-Forwarded-Proto"].FirstOrDefault();

        string? requestOrigin = origin;

        // If no direct origin, try to construct from forwarded headers
        if (string.IsNullOrWhiteSpace(requestOrigin) && 
            !string.IsNullOrWhiteSpace(forwardedHost) && 
            !string.IsNullOrWhiteSpace(forwardedProto))
        {
            requestOrigin = $"{forwardedProto}://{forwardedHost}";
        }

        // Fallback to referer
        if (string.IsNullOrWhiteSpace(requestOrigin) && !string.IsNullOrWhiteSpace(referer))
        {
            if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            {
                requestOrigin = $"{refererUri.Scheme}://{refererUri.Authority}";
            }
        }

        // =================================================================
        // STEP 3: If no origin could be determined, reject (unless from trusted network)
        // =================================================================
        if (string.IsNullOrWhiteSpace(requestOrigin))
        {
            _logger.LogWarning(
                "State-changing request to {Path} from {IpAddress} has no Origin, Referer, or X-Forwarded headers",
                request.Path,
                remoteIp);
            return false;
        }

        // =================================================================
        // STEP 4: Validate origin against allowed list
        // =================================================================
        var normalizedOrigin = NormalizeOrigin(requestOrigin);

        if (_allowedOrigins.Contains(normalizedOrigin))
        {
            return true;
        }

        // Check if origin matches the host (same-origin request)
        var requestHost = $"{request.Scheme}://{request.Host}";
        if (normalizedOrigin.Equals(NormalizeOrigin(requestHost), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Log the failure with all details for debugging
        _logger.LogWarning(
            "Origin validation FAILED. " +
            "Detected Origin: {Origin}, " +
            "Path: {Path}, " +
            "RemoteIP: {IpAddress}, " +
            "Allowed origins: [{AllowedOrigins}]",
            requestOrigin,
            request.Path,
            remoteIp,
            string.Join(", ", _allowedOrigins));

        return false;
    }

    private static bool IsStateChangingRequest(string method)
    {
        return method is "POST" or "PUT" or "DELETE" or "PATCH";
    }

    private static bool IsAuthenticationEndpoint(PathString path)
    {
        return path.StartsWithSegments("/api/v1/auth", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/api/v2/auth", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeOrigin(string origin)
    {
        return origin.TrimEnd('/').ToLowerInvariant();
    }

    private bool IsFromTrustedNetwork(System.Net.IPAddress ipAddress)
    {
        var trustedIPs = _configuration.GetSection("Security:TrustedProxyIPs").Get<string[]>()
            ?? [];

        foreach (var trustedIP in trustedIPs)
        {
            if (System.Net.IPAddress.TryParse(trustedIP, out var trusted) && 
                ipAddress.Equals(trusted))
            {
                _logger.LogDebug("IP {IpAddress} matches trusted proxy {TrustedIP}", ipAddress, trustedIP);
                return true;
            }
        }

        return false;
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
