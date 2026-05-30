using Asp.Versioning;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Interswitch.Converters;
using AegisEInvoicing.ERP.API.Filters;
using AegisEInvoicing.ERP.API.Models;
using AegisEInvoicing.ERP.API.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.OpenApi;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace AegisEInvoicing.ERP.API.Extensions;

/// <summary>
/// Service collection extensions for API configuration
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // HTTP Context
        services.AddHttpContextAccessor();

        // =================================================================
        // FLUENT VALIDATION REGISTRATION
        // =================================================================
        // VAPT: Register API-specific validators for comprehensive input validation
        // These validators implement security-focused input sanitization
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // =================================================================
        // RESPONSE INTEGRITY SERVICES
        // =================================================================
        // Addresses VAPT finding: Response tampering vulnerability
        // Provides HMAC-SHA256 signing of responses and server-side action validation
        services.AddMemoryCache();
        services.AddSingleton<IResponseIntegrityService, ResponseIntegrityService>();
        services.AddSingleton<IServerSideActionValidator, ServerSideActionValidator>();
        services.Configure<ResponseIntegrityOptions>(configuration.GetSection("Security:ResponseIntegrity"));

        // API Controllers
        services.AddControllers(options =>
        {
            options.Filters.Add<GlobalExceptionFilter>();
            options.Filters.Add<UnknownPropertiesValidationFilter>(); // BOPLA protection - reject unknown properties
            options.Filters.Add<ValidationFilter>();
            options.Filters.Add<ApiResponseFilter>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.Converters.Add(new InterswitchResponseConverterFactory());
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
            options.JsonSerializerOptions.AllowTrailingCommas = true;
            options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                var errorMessages = errors.Select(e => $"{e.Key}: {string.Join(", ", e.Value)}");
                var response = new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Validation failed: {string.Join("; ", errorMessages)}",
                    Data = errors
                };

                return new BadRequestObjectResult(response);
            };
        });

        // API Versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("x-api-version"),
                new MediaTypeApiVersionReader("x-api-version"));
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Authentication & Authorization - API Key authentication
        // Using API keys directly as requested by client

        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
            .AddPolicy("KMPGAdminOnly", policy => policy.RequireRole("KMPGAdmin"))
            .AddPolicy("OrganizationAdminOnly", policy => policy.RequireRole("OrganizationAdmin"));

        // OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddOpenApi("v1");

        // =================================================================
        // CORS CONFIGURATION
        // =================================================================
        // Addresses VAPT finding: Insecure CORS
        // Ensures only trusted origins can access the API with credentials
        // IMPORTANT: Never use wildcard "*" or AllowAnyOrigin with AllowCredentials
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigins", builder =>
            {
                // Get allowed origins from configuration
                // SECURITY: No wildcard fallback - explicit origins only
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

                // If no origins configured, throw exception (fail-secure approach)
                if (allowedOrigins == null || allowedOrigins.Length == 0)
                {
                    throw new InvalidOperationException(
                        "CORS configuration error: No allowed origins specified in configuration. " +
                        "Please configure 'Cors:AllowedOrigins' in appsettings.json or .env file. " +
                        "Never use wildcard '*' with AllowCredentials.");
                }

                // Validate that wildcard is not used with credentials
                if (allowedOrigins.Contains("*"))
                {
                    throw new InvalidOperationException(
                        "CORS security violation: Wildcard '*' origin is not allowed with credentials. " +
                        "Please specify explicit trusted origins in 'Cors:AllowedOrigins' configuration.");
                }

                // Get allowed methods from configuration (secure defaults)
                var allowedMethods = configuration.GetSection("Cors:AllowedMethods").Get<string[]>()
                    ?? new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };

                // Get allowed headers from configuration (secure defaults - no wildcard)
                var allowedHeaders = configuration.GetSection("Cors:AllowedHeaders").Get<string[]>()
                    ?? new[] { "Content-Type", "Authorization", "X-API-Key", "X-Request-ID" };

                // Get credentials setting (default: false for security)
                var allowCredentials = configuration.GetValue<bool>("Cors:AllowCredentials", false);

                builder.WithOrigins(allowedOrigins)
                    .WithMethods(allowedMethods)
                    .WithHeaders(allowedHeaders)
                    .WithExposedHeaders(
                        "Token-Expired",
                        "X-Pagination",
                        // Response integrity headers for client-side verification
                        "X-Response-Signature",
                        "X-Response-Timestamp",
                        "X-Response-Nonce",
                        "X-Response-Hash",
                        "X-Response-Signature-Algorithm",
                        "X-Response-Content-Length");

                if (allowCredentials)
                {
                    builder.AllowCredentials();
                }
            });
        });

        // Rate Limiting
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.AddPolicy("ApiLimit", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // Add ApiOnly policy for SaaS API controllers
            options.AddPolicy("ApiOnly", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:ApiOnly:PermitLimit", 100),
                        Window = TimeSpan.Parse(configuration["RateLimiting:ApiOnly:Window"] ?? "00:01:00")
                    }));

            // =================================================================
            // VAPT: Invoice Creation Rate Limiting
            // =================================================================
            // Addresses VAPT finding: Request Flooding on invoice creation
            // STRICT: Maximum 10 invoices per user per minute
            // Uses sliding window for smoother rate limiting
            options.AddPolicy("InvoiceCreation", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetUserBasedKey(context),
                    factory: partition => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:InvoiceCreation:PermitLimit", 10),
                        Window = TimeSpan.Parse(configuration["RateLimiting:InvoiceCreation:Window"] ?? "00:01:00"),
                        SegmentsPerWindow = 6 // 10-second segments for smoother rate limiting
                    }));

            // Invoice Operations (Validate, Sign, Transmit) - 20 per minute per user
            options.AddPolicy("InvoiceOperations", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetUserBasedKey(context),
                    factory: partition => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:InvoiceOperations:PermitLimit", 20),
                        Window = TimeSpan.Parse(configuration["RateLimiting:InvoiceOperations:Window"] ?? "00:01:00"),
                        SegmentsPerWindow = 4
                    }));

            // =================================================================
            // CONSOLIDATED INVOICE SUBMISSION - Create + Validate + Sign + Transmit
            // =================================================================
            // Stricter than individual operations since it executes 4 steps
            // Maximum 5 consolidated submissions per minute per user
            options.AddPolicy("ConsolidatedInvoiceSubmission", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetUserBasedKey(context),
                    factory: partition => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:ConsolidatedInvoiceSubmission:PermitLimit", 5),
                        Window = TimeSpan.Parse(configuration["RateLimiting:ConsolidatedInvoiceSubmission:Window"] ?? "00:01:00"),
                        SegmentsPerWindow = 6 // 10-second segments
                    }));

            // Bulk Operations - Very restrictive: 5 per 5 minutes per user
            options.AddPolicy("BulkOperations", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetUserBasedKey(context),
                    factory: partition => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:BulkOperations:PermitLimit", 5),
                        Window = TimeSpan.Parse(configuration["RateLimiting:BulkOperations:Window"] ?? "00:05:00"),
                        SegmentsPerWindow = 5
                    }));

            // =================================================================
            // CONSOLIDATED BULK SUBMISSION - Bulk Create + Validate + Sign + Transmit
            // =================================================================
            // Very restrictive: 2 per 5 minutes per user (heavier than regular bulk)
            options.AddPolicy("ConsolidatedBulkSubmission", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetUserBasedKey(context),
                    factory: partition => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:ConsolidatedBulkSubmission:PermitLimit", 2),
                        Window = TimeSpan.Parse(configuration["RateLimiting:ConsolidatedBulkSubmission:Window"] ?? "00:05:00"),
                        SegmentsPerWindow = 5
                    }));

            // On rate limit exceeded - return detailed error with retry information
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                // Add rate limit headers
                var hasRetryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter);
                var retryAfterSeconds = hasRetryAfter ? retryAfter.TotalSeconds : 60;

                if (hasRetryAfter)
                {
                    context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
                }

                var errorResponse = new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = 429,
                    detail = "Rate limit exceeded. Please reduce the frequency of your requests.",
                    retryAfter = retryAfterSeconds,
                    traceId = context.HttpContext.TraceIdentifier
                };

                await context.HttpContext.Response.WriteAsJsonAsync(errorResponse, token);
            };
        });

        // Response Compression
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        // Response Caching
        services.AddResponseCaching();

        // Health Checks
        services.AddHealthChecks()
            .AddCheck("database", () => HealthCheckResult.Healthy("Database is available"))
            .AddCheck<HealthChecks.SubscriptionHealthCheck>(
                "subscription",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "unhealthy"])
            .AddRedis(
                configuration.GetConnectionString("Redis") ?? string.Empty,
                name: "redis",
                failureStatus: HealthStatus.Degraded)
            .AddRabbitMQ(
                name: "rabbitmq",
                failureStatus: HealthStatus.Degraded)
            .AddUrlGroup(
                options =>
                {
                    options.AddUri(new Uri(configuration["ExternalApi:HealthCheckUrl"] ?? "https://api.example.com/health"));
                },
                name: "external-api",
                failureStatus: HealthStatus.Degraded);

        return services;
    }

    public static IServiceCollection AddOpenTelemetryServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add simplified OpenTelemetry - can be configured properly later
        services.AddOpenTelemetry();

        return services;
    }

    /// <summary>
    /// Gets a user-based rate limit key for authenticated requests.
    /// Falls back to IP address for anonymous requests.
    /// </summary>
    private static string GetUserBasedKey(HttpContext context)
    {
        // Prefer authenticated user identity
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst("id")?.Value
                ?? context.User.Identity.Name;

            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }
        }

        // Fall back to IP address for unauthenticated requests
        return $"ip:{GetIpAddress(context)}";
    }

    /// <summary>
    /// Gets the client IP address, considering X-Forwarded-For header for proxied requests.
    /// </summary>
    private static string GetIpAddress(HttpContext context)
    {
        // Check for forwarded IP (from reverse proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first (original client)
            var clientIp = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(clientIp))
            {
                return clientIp;
            }
        }

        // Use connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}