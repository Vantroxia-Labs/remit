using System.Text;
using System.Text.Json;
using AegisEInvoicing.ERP.API.Security;

namespace AegisEInvoicing.ERP.API.Middleware;

/// <summary>
/// Middleware to add response integrity signatures to API responses.
/// Addresses VAPT finding: Response tampering vulnerability.
///
/// This middleware:
/// 1. Captures the response body
/// 2. Generates HMAC-SHA256 signature of the response
/// 3. Adds integrity headers to prevent tampering
/// 4. Includes anti-replay nonce
///
/// Headers added:
/// - X-Response-Signature: HMAC-SHA256 signature of the response
/// - X-Response-Timestamp: UTC timestamp of response generation
/// - X-Response-Nonce: Unique nonce for anti-replay protection
/// - X-Response-Hash: SHA256 hash of response body
/// </summary>
public class ResponseIntegrityMiddleware(
    RequestDelegate next,
    ILogger<ResponseIntegrityMiddleware> logger,
    IConfiguration configuration)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ResponseIntegrityMiddleware> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    // Paths to exclude from response signing
    private static readonly HashSet<string> ExcludedPathPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/swagger",
        "/favicon.ico"
    };

    public async Task InvokeAsync(HttpContext context, IResponseIntegrityService integrityService)
    {
        // Check if response signing is enabled
        var isEnabled = _configuration.GetValue("Security:ResponseIntegrity:Enabled", true);
        if (!isEnabled)
        {
            await _next(context);
            return;
        }

        // Skip signing for excluded paths
        var path = context.Request.Path.Value ?? string.Empty;
        if (ShouldExcludePath(path))
        {
            await _next(context);
            return;
        }

        // Skip for non-API requests (based on Accept header or path)
        if (!IsApiRequest(context))
        {
            await _next(context);
            return;
        }

        // Get the request ID for correlation
        var requestId = context.TraceIdentifier;

        // Generate nonce before processing
        var nonce = integrityService.GenerateNonce();

        // Capture the original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            // Use a memory stream to capture the response
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            // Process the request
            await _next(context);

            // Reset stream position to read the response
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();

            // Only sign successful JSON responses
            if (ShouldSignResponse(context, responseBody))
            {
                // Generate timestamp
                var timestamp = DateTime.UtcNow;

                // Generate signature
                var signature = integrityService.GenerateSignature(
                    responseBody,
                    timestamp,
                    nonce,
                    requestId);

                // Add integrity headers
                AddIntegrityHeaders(context, signature, timestamp, nonce, responseBody);

                // Record nonce usage to prevent replay
                integrityService.RecordNonceUsage(nonce);

                _logger.LogDebug(
                    "Added response integrity headers for {Method} {Path} with nonce {Nonce}",
                    context.Request.Method,
                    context.Request.Path,
                    nonce);
            }

            // Copy the response to the original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            // Restore the original stream
            context.Response.Body = originalBodyStream;
        }
    }

    private bool ShouldExcludePath(string path)
    {
        // Check configured excluded paths
        var configuredExclusions = _configuration
            .GetSection("Security:ResponseIntegrity:ExcludedPaths")
            .Get<List<string>>() ?? new List<string>();

        foreach (var exclusion in ExcludedPathPrefixes.Concat(configuredExclusions))
        {
            if (path.StartsWith(exclusion, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsApiRequest(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Check if it's an API path
        if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check Accept header for JSON
        var acceptHeader = context.Request.Headers.Accept.ToString();
        if (acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool ShouldSignResponse(HttpContext context, string responseBody)
    {
        // Only sign responses with content
        if (string.IsNullOrEmpty(responseBody))
        {
            return false;
        }

        // Only sign JSON responses
        var contentType = context.Response.ContentType;
        if (string.IsNullOrEmpty(contentType) ||
            !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Sign all status codes (including errors, as they can also be tampered)
        return true;
    }

    private void AddIntegrityHeaders(
        HttpContext context,
        string signature,
        DateTime timestamp,
        string nonce,
        string responseBody)
    {
        var headers = context.Response.Headers;

        // Primary signature header
        headers["X-Response-Signature"] = signature;

        // Timestamp for freshness validation
        headers["X-Response-Timestamp"] = timestamp.ToString("O");

        // Nonce for anti-replay protection
        headers["X-Response-Nonce"] = nonce;

        // Optionally include body hash for quick integrity check
        if (_configuration.GetValue("Security:ResponseIntegrity:IncludeBodyHash", true))
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(responseBody));
            headers["X-Response-Hash"] = Convert.ToBase64String(hashBytes);
        }

        // Add algorithm identifier for transparency
        headers["X-Response-Signature-Algorithm"] = "HMAC-SHA256";

        // Add content length for additional validation
        headers["X-Response-Content-Length"] = Encoding.UTF8.GetByteCount(responseBody).ToString();
    }
}

/// <summary>
/// Extension methods for registering response integrity middleware
/// </summary>
public static class ResponseIntegrityMiddlewareExtensions
{
    public static IApplicationBuilder UseResponseIntegrity(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ResponseIntegrityMiddleware>();
    }
}
