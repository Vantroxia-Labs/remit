using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace AegisEInvoicing.ERP.API.Middleware;

/// <summary>
/// Middleware that prevents duplicate request processing using idempotency keys.
/// Addresses request flooding and duplicate invoice creation
/// Ensures critical operations (invoice create/sign/transmit) are processed only once per idempotency key.
/// </summary>
public class IdempotencyMiddleware(
    RequestDelegate next,
    ILogger<IdempotencyMiddleware> logger,
    IConfiguration configuration)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILogger<IdempotencyMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public async Task InvokeAsync(HttpContext context, IMemoryCache cache)
    {
        // Check if idempotency is enabled
        var isEnabled = _configuration.GetValue<bool>("Idempotency:Enabled", true);

        if (!isEnabled || !RequiresIdempotency(context))
        {
            await _next(context);
            return;
        }

        // Extract idempotency key from header
        var idempotencyKey = context.Request.Headers["X-Idempotency-Key"].FirstOrDefault();

        // If key is missing or invalid, do NOT alter the response.
        // Just log and pass the request through unchanged so downstream errors are preserved.
        // Good days are getting better
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            _logger.LogDebug(
                "Idempotency (soft): Missing idempotency key for {Method} {Path}. Passing through.",
                context.Request.Method,
                context.Request.Path);

            await _next(context);
            return;
        }

        // Validate idempotency key format (UUID or similar). If invalid, pass through unchanged.
        if (!IsValidIdempotencyKey(idempotencyKey))
        {
            _logger.LogDebug(
                "Idempotency (soft): Invalid idempotency key format '{Key}' for {Method} {Path}. Passing through.",
                idempotencyKey,
                context.Request.Method,
                context.Request.Path);

            await _next(context);
            return;
        }

        // Create cache key: userId + endpoint + idempotencyKey
        var userId = context.User?.Identity?.Name ?? "anonymous";
        var endpoint = $"{context.Request.Method}:{context.Request.Path}";
        var cacheKey = $"idempotency:{userId}:{endpoint}:{idempotencyKey}";

        try
        {
            // Check if request with this idempotency key already exists
            if (cache.TryGetValue(cacheKey, out string? existingResponse) && !string.IsNullOrEmpty(existingResponse))
            {
                _logger.LogInformation(
                    "Idempotency: Duplicate request detected. Returning cached response for key: {Key}",
                    idempotencyKey);

                // Return cached response
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                context.Response.Headers["X-Idempotent-Replay"] = "true";
                await context.Response.WriteAsync(existingResponse);
                return;
            }

            // Capture the response
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Process the request
            await _next(context);

            // Only cache successful responses (2xx status codes)
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);

                if (!string.IsNullOrEmpty(responseText))
                {
                    // Cache the response for 24 hours (configurable)
                    var expirationMinutes = _configuration.GetValue<int>("Idempotency:CacheExpirationMinutes", 1440);
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes),
                        Priority = CacheItemPriority.Normal
                    };

                    cache.Set(cacheKey, responseText, cacheOptions);

                    _logger.LogInformation(
                        "Idempotency: Cached response for key: {Key} (expires in {Minutes} minutes)",
                        idempotencyKey,
                        expirationMinutes);
                }
            }

            // Copy the (unaltered) response back to the original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in idempotency middleware");
            throw;
        }
    }

    private static bool RequiresIdempotency(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = context.Request.Method.ToUpperInvariant();

        // Only require idempotency for mutating operations (POST, PUT, PATCH)
        if (method != "POST" && method != "PUT" && method != "PATCH")
        {
            return false;
        }

        // Define endpoints that require idempotency
        var idempotentEndpoints = new[]
        {
            "/api/v1/firs/create-and-submit-invoice",
            "/api/v1/firs/create-invoice",
            "/api/v1/firs/validate",
            "/api/v1/firs/sign",
            "/api/v1/firs/transmit",
            "/api/v1/firs/update-payment-status",
            "/api/v1/systemintegrator/create-invoice",
            "/api/v1/systemintegrator/validate",
            "/api/v1/systemintegrator/sign",
            "/api/v1/systemintegrator/transmit"
        };

        return idempotentEndpoints.Any(ep => path.Contains(ep));
    }

    private static bool IsValidIdempotencyKey(string key)
    {
        // Accept UUIDs or alphanumeric strings of reasonable length
        if (key.Length < 16 || key.Length > 128)
            return false;

        // Try parse as GUID
        if (Guid.TryParse(key, out _))
            return true;

        // Or check if it's alphanumeric with hyphens/underscores
        return key.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
    }

    private static async Task WriteErrorResponse(HttpContext context, string message, int statusCode)
    {
        // Check if response has already started
        if (context.Response.HasStarted)
        {
            return;
        }

        // Clear any existing response
        context.Response.Clear();
        
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            Success = false,
            Message = message,
            ErrorCode = "IDEMPOTENCY_REQUIRED",
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
/// Extension methods for registering idempotency middleware
/// </summary>
public static class IdempotencyMiddlewareExtensions
{
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IdempotencyMiddleware>();
    }
}