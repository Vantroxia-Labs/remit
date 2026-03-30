using AegisEInvoicing.ERP.API.Models;
using System.Text.Json;

namespace AegisEInvoicing.ERP.API.Middleware;

/// <summary>
/// Middleware to validate required API headers and return clear error messages
/// Addresses: Missing header validation with detailed error responses
/// </summary>
public class ApiHeaderValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiHeaderValidationMiddleware> _logger;

    public ApiHeaderValidationMiddleware(
        RequestDelegate next,
        ILogger<ApiHeaderValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate API endpoints (skip health checks, swagger, etc.)
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Skip validation for GET requests to swagger/health endpoints
        if (context.Request.Path.StartsWithSegments("/api/swagger") ||
            context.Request.Path.StartsWithSegments("/api/health"))
        {
            await _next(context);
            return;
        }

        var errors = new List<string>();

        // 1. Validate X-API-Key header
        if (!context.Request.Headers.ContainsKey("X-API-Key"))
        {
            errors.Add("Missing required header 'X-API-Key'. Please provide your API key in the X-API-Key header.");
        }
        else if (string.IsNullOrWhiteSpace(context.Request.Headers["X-API-Key"]))
        {
            errors.Add("Header 'X-API-Key' cannot be empty. Please provide a valid API key.");
        }

        // 2. Validate Content-Type for POST/PUT/PATCH requests WITH a request body
        if (context.Request.Method is "POST" or "PUT" or "PATCH")
        {
            // Only validate Content-Type if the request has a body (Content-Length > 0)
            var contentLength = context.Request.ContentLength;
            var hasBody = contentLength.HasValue && contentLength.Value > 0;
            
            if (hasBody)
            {
                var contentType = context.Request.ContentType;
                
                if (string.IsNullOrWhiteSpace(contentType))
                {
                    errors.Add("Missing required header 'Content-Type'. Please set Content-Type to 'application/json' for request body.");
                }
                else if (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Invalid Content-Type '{contentType}'. Please set Content-Type to 'application/json'.");
                }
            }
        }

        // If there are validation errors, return immediately with detailed message
        if (errors.Any())
        {
            _logger.LogWarning("API header validation failed for {Method} {Path}: {Errors}",
                context.Request.Method,
                context.Request.Path,
                string.Join("; ", errors));

            await ReturnHeaderValidationError(context, errors);
            return;
        }

        // Headers are valid, continue to next middleware
        await _next(context);
    }

    private static async Task ReturnHeaderValidationError(HttpContext context, List<string> errors)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";

        var errorMessage = errors.Count == 1
            ? errors[0]
            : $"Multiple header validation errors: {string.Join(" ", errors)}";

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = errorMessage,
            Data = new
            {
                MissingHeaders = errors,
                Hint = "Please check your request headers and ensure all required headers are present with valid values."
            },
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Extension method to register the middleware
/// </summary>
public static class ApiHeaderValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseApiHeaderValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiHeaderValidationMiddleware>();
    }
}
