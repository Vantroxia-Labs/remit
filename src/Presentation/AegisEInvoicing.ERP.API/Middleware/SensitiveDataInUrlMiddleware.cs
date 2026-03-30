using Microsoft.Extensions.Primitives;

namespace AegisEInvoicing.ERP.API.Middleware;

/// <summary>
/// Middleware to prevent sensitive data (passwords, tokens, etc.) from being transmitted in URL query strings
/// Addresses VAPT finding: Password submitted using GET method
/// Protects against: Credential exposure in logs, browser history, and proxy logs
/// </summary>
public class SensitiveDataInUrlMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SensitiveDataInUrlMiddleware> _logger;
    private readonly HashSet<string> _sensitiveParameters;

    public SensitiveDataInUrlMiddleware(
        RequestDelegate next,
        ILogger<SensitiveDataInUrlMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        // List of sensitive parameter names that should NEVER appear in query strings
        // This list uses case-insensitive comparison
        _sensitiveParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Password-related
            "password",
            "passwd",
            "pwd",
            "pass",
            "newpassword",
            "oldpassword",
            "currentpassword",
            "confirmpassword",
            "newpwd",
            "oldpwd",

            // Authentication tokens
            "token",
            "accesstoken",
            "refreshtoken",
            "authtoken",
            "bearer",
            "jwt",
            // Note: "apikey" and "api_key" are intentionally NOT blocked here
            // because API keys are designed for URL-based authentication (query string fallback)
            // They are not secrets like passwords - they identify the caller
            // The API key authentication handler supports both header and query string
            "secret",
            "secretkey",
            "api_secret",

            // Credit card and payment info
            "creditcard",
            "cardnumber",
            "cvv",
            "cvc",
            "card",
            "ccnumber",
            "cardnum",
            "pan",

            // Social security and personal identifiers
            "ssn",
            "socialsecurity",
            "taxid",
            "nin",
            "nationalid",

            // Banking information
            "accountnumber",
            "routingnumber",
            "iban",
            "swift",
            "bic",

            // Other sensitive data
            "pin",
            "otp",
            "verification_code",
            "verificationcode",
            "securitycode",
            "privateke",
            "privatekey",
            "encryptionkey"
        };

        _logger.LogInformation(
            "Sensitive Data in URL Protection initialized. Monitoring {Count} sensitive parameter names",
            _sensitiveParameters.Count);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only check GET requests (POST/PUT/DELETE should use request body)
        if (context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
        {
            var queryString = context.Request.QueryString.Value;

            if (!string.IsNullOrWhiteSpace(queryString))
            {
                // Check if any query parameter matches sensitive parameter names
                var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString);

                foreach (var param in query)
                {
                    if (_sensitiveParameters.Contains(param.Key))
                    {
                        // CRITICAL SECURITY VIOLATION: Sensitive data in URL
                        _logger.LogError(
                            "SECURITY VIOLATION: Sensitive parameter '{ParameterName}' detected in URL query string. " +
                            "Method: {Method}, Path: {Path}, IP: {IpAddress}, UserAgent: {UserAgent}. " +
                            "This is a critical security issue - credentials and sensitive data must NEVER be transmitted in URLs.",
                            param.Key,
                            context.Request.Method,
                            context.Request.Path,
                            context.Connection.RemoteIpAddress,
                            context.Request.Headers.UserAgent.ToString());

                        // Block the request immediately
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        context.Response.ContentType = "application/json";

                        // Remove any headers that might disclose server information
                        context.Response.Headers.Remove("Server");
                        context.Response.Headers.Remove("X-Powered-By");

                        var errorResponse = new
                        {
                            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                            title = "Bad Request",
                            status = 400,
                            detail = "Sensitive data cannot be transmitted in URL query strings. " +
                                     "Please use HTTP POST/PUT with request body for sensitive operations. " +
                                     "This is a security requirement to prevent credential exposure in logs and browser history.",
                            traceId = context.TraceIdentifier,
                            securityNote = "Passwords, tokens, and other sensitive data must be sent in the request body, not in URL parameters."
                        };

                        await context.Response.WriteAsJsonAsync(errorResponse);
                        return;
                    }
                }
            }
        }

        // Also check all HTTP methods for sensitive data in route parameters
        // Example: /api/users/password/{password} - THIS IS ALSO DANGEROUS
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Check if any segment of the path contains sensitive keywords
        if (ContainsSensitivePathSegment(path))
        {
            _logger.LogWarning(
                "SECURITY WARNING: Potentially sensitive data in URL path: {Path}, Method: {Method}, IP: {IpAddress}",
                context.Request.Path,
                context.Request.Method,
                context.Connection.RemoteIpAddress);

            // Log warning but allow request to proceed (might be false positive)
            // If this is a real issue, it should be caught and fixed in development
        }

        await _next(context);
    }

    /// <summary>
    /// Checks if the URL path contains sensitive keywords that indicate credentials might be in the path
    /// </summary>
    private bool ContainsSensitivePathSegment(string path)
    {
        // Check for common patterns like: /password/value or /token/value
        // This is a heuristic check and may have false positives
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in pathSegments)
        {
            // Skip common segment names that are safe
            if (segment == "password" || segment == "token" || segment == "change-password" ||
                segment == "reset-password" || segment == "forgot-password")
                continue; // These are endpoint names, not values

            // Check if segment looks like it might contain sensitive data
            // (e.g., very long strings that might be passwords or tokens)
            if (segment.Length > 20 && !segment.Contains('-') && !segment.Contains('_'))
            {
                // Might be a password/token value in the path
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Extension methods for registering sensitive data in URL protection middleware
/// </summary>
public static class SensitiveDataInUrlMiddlewareExtensions
{
    public static IApplicationBuilder UseSensitiveDataProtection(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SensitiveDataInUrlMiddleware>();
    }
}
