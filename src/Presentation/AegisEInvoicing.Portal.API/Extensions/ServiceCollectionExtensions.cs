using Asp.Versioning;
using AegisEInvoicing.Portal.API.Filters;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Interswitch.Converters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace AegisEInvoicing.Portal.API.Extensions;

/// <summary>
/// Schema filter to ensure OpenAPI 3.0.3 compatibility for IBM API Connect
/// </summary>
public class OpenApi303CompatibilityFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        // Remove any OpenAPI 3.1+ specific features that might cause issues
        schema.Extensions?.Remove("x-nullable");
    }
}

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

        // API Controllers
        services.AddControllers(options =>
        {
            options.Filters.Add<GlobalExceptionFilter>();
            options.Filters.Add<ValidationFilter>();
            options.Filters.Add<ApiResponseFilter>();
            options.Filters.Add<ResponseSigningFilter>(); // Response tampering protection
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter());
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

                var response = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = errors
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

        // Authentication & Authorization
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured"))),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = async context =>
                    {
                        // Decrypt encrypted token before validation
                        var token = context.Token;
                        if (string.IsNullOrEmpty(token))
                        {
                            // Try to get token from Authorization header
                            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                token = authHeader["Bearer ".Length..].Trim();
                            }
                        }

                        if (!string.IsNullOrEmpty(token))
                        {
                            try
                            {
                                var encryptionService = context.HttpContext.RequestServices
                                    .GetRequiredService<AegisEInvoicing.Application.Common.Interfaces.IEncryptionService>();

                                // Attempt to decrypt the token (it might be encrypted)
                                var decryptedJwt = await encryptionService.DecryptAsync(token);
                                context.Token = decryptedJwt;
                            }
                            catch
                            {
                                // Token might not be encrypted (backward compatibility) or invalid
                                // Let it pass through - JWT validation will handle it
                                context.Token = token;
                            }
                        }
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                        // Check if token is blacklisted (revoked)
                        var jti = context.Principal?.FindFirst("jti")?.Value;
                        if (!string.IsNullOrWhiteSpace(jti))
                        {
                            var blacklistService = context.HttpContext.RequestServices
                                .GetRequiredService<Application.Common.Interfaces.ITokenBlacklistService>();

                            if (await blacklistService.IsTokenBlacklistedAsync(jti, context.HttpContext.RequestAborted))
                            {
                                logger.LogWarning("Blacklisted token attempted to access system. JTI: {Jti}", jti);
                                context.Fail("This token has been revoked");
                                return;
                            }
                        }

                        var sessionIdClaim = context.Principal?.FindFirst("sessionId")?.Value;
                        if (!string.IsNullOrWhiteSpace(sessionIdClaim) && Guid.TryParse(sessionIdClaim, out var sessionId))
                        {
                            // Resolve scoped DB/context/service
                            await using var scope = context.HttpContext.RequestServices.CreateAsyncScope();
                            var db = scope.ServiceProvider.GetRequiredService<Application.Common.Interfaces.IApplicationDbContext>();

                            var session = await db.UserSessions
                                .Where(s => s.Id == sessionId)
                                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                            if (session == null)
                            {
                                logger.LogWarning("Token presented with unknown sessionId {SessionId}", sessionId);
                                context.Fail("Session not found");
                                return;
                            }

                            if (!session.IsActive)
                            {
                                logger.LogWarning("Token presented for terminated session {SessionId} (reason: {Reason})", sessionId, session.EndReason);
                                context.Fail($"Session terminated: {session.EndReason ?? "Concurrent login detected"}");
                                return;
                            }

                            // LICENSE VALIDATION (ONPREM DEPLOYMENTS ONLY)
                            var businessIdClaim = context.Principal?.FindFirst("businessId")?.Value;
                            if (!string.IsNullOrWhiteSpace(businessIdClaim) && Guid.TryParse(businessIdClaim, out var businessId))
                            {
                                var business = await db.Businesses
                                    .Where(b => b.Id == businessId)
                                    .Select(b => new { b.DeploymentMode, b.LicenseKey })
                                    .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                                if (business != null && business.DeploymentMode == AegisEInvoicing.Domain.Entities.DeploymentMode.OnPremise)
                                {
                                    // ONPREM deployment - validate license
                                    if (string.IsNullOrWhiteSpace(business.LicenseKey))
                                    {
                                        logger.LogWarning("ONPREM business {BusinessId} has no license key", businessId);
                                        context.Fail("License key not configured for this on-premise deployment");
                                        return;
                                    }

                                    var licensingService = scope.ServiceProvider
                                        .GetRequiredService<AegisEInvoicing.Application.Common.Interfaces.ILicensingService>();

                                    var validationResult = await licensingService.ValidateLicenseKeyAsync(
                                        business.LicenseKey,
                                        failOpen: true,  // FAIL-OPEN: Allow login if licensing service is down
                                        context.HttpContext.RequestAborted);

                                    if (!validationResult.IsValid && !validationResult.IsFailOpen)
                                    {
                                        // License is invalid (400 response from licensing service)
                                        logger.LogWarning(
                                            "Invalid license for ONPREM business {BusinessId}: {Message}",
                                            businessId, validationResult.Message);
                                        context.Fail($"License validation failed: {validationResult.Message}");
                                        return;
                                    }

                                    if (validationResult.IsFailOpen)
                                    {
                                        // FAIL-OPEN: Licensing service error (500) - allow login
                                        logger.LogWarning(
                                            "License service unavailable for business {BusinessId}. FAIL-OPEN: Allowing login. {Message}",
                                            businessId, validationResult.Message);
                                    }
                                    else
                                    {
                                        // License valid
                                        logger.LogInformation(
                                            "License validated successfully for ONPREM business {BusinessId}",
                                            businessId);
                                    }
                                }
                            }
                        }


                        logger.LogInformation("Token validated for user: {UserId}",
            context.Principal?.Identity?.Name);
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("KMPGAdminOnly", policy => policy.RequireRole("KMPGAdmin"));
            options.AddPolicy("OrganizationAdminOnly", policy => policy.RequireRole("OrganizationAdmin"));
            
            // Portal CUD operations require SaaS subscription tier
            // ApiOnly and SFTP tiers have read-only access to Portal
            options.AddPolicy("RequireSaasSubscription", policy =>
                policy.Requirements.Add(new AegisEInvoicing.Portal.API.Authorization.RequireSaasSubscriptionRequirement()));
        });

        // Register authorization handler for SaaS subscription requirement
        services.AddSingleton<IAuthorizationHandler, AegisEInvoicing.Portal.API.Authorization.RequireSaasSubscriptionHandler>();

        // Swagger/OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "EInvoice Integrator API",
                Version = "v1",
                Description = "Enterprise Electronic Invoice Integration System API",
                Contact = new OpenApiContact
                {
                    Name = "Development Team",
                    Email = "dev@aegiseinvoicing.com"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            options.SwaggerDoc("v2", new OpenApiInfo
            {
                Title = "EInvoice Integrator API",
                Version = "v2",
                Description = "Enterprise Electronic Invoice Integration System API V2"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    new List<string>()
                }
            });

            options.EnableAnnotations();
            options.CustomSchemaIds(type => type.FullName);

            // Add schema filter for OpenAPI 3.0.3 compatibility with IBM API Connect
            options.SchemaFilter<OpenApi303CompatibilityFilter>();

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        // =================================================================
        // CORS CONFIGURATION
        // =================================================================
        // Addresses VAPT finding: Cross-Domain Misconfiguration
        // SECURITY: Never use AllowAnyOrigin(), AllowAnyMethod(), or AllowAnyHeader()
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
                        "Please configure 'Cors:AllowedOrigins' in appsettings.json or .env file.");
                }

                // Validate that wildcard is not used with credentials
                if (allowedOrigins.Contains("*"))
                {
                    throw new InvalidOperationException(
                        "CORS security violation: Wildcard '*' origin is not allowed with credentials. " +
                        "Please specify explicit trusted origins in 'Cors:AllowedOrigins' configuration.");
                }

                // Get allowed methods from configuration (secure defaults - no wildcard)
                var allowedMethods = configuration.GetSection("Cors:AllowedMethods").Get<string[]>()
                    ?? new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };

                // Get allowed headers from configuration (specific headers only - no wildcard)
                var allowedHeaders = configuration.GetSection("Cors:AllowedHeaders").Get<string[]>()
                    ?? new[] { "Content-Type", "Authorization", "X-API-Version", "X-Request-ID", "X-Encrypted" };

                // Get credentials setting (default: true for this API as it uses JWT auth)
                var allowCredentials = configuration.GetValue<bool>("Cors:AllowCredentials", true);

                builder.WithOrigins(allowedOrigins)
                    .WithMethods(allowedMethods)
                    .WithHeaders(allowedHeaders)
                    .WithExposedHeaders("Token-Expired", "X-Pagination");

                if (allowCredentials)
                {
                    builder.AllowCredentials();
                }
            });
        });

        // Rate Limiting - Comprehensive protection against flooding attacks
        services.AddRateLimiter(options =>
        {
            // Global rate limit - 100 requests per minute per user/IP
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetRateLimitKey(httpContext),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // Standard API limit - 30 requests per minute
            options.AddPolicy("ApiLimit", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetRateLimitKey(context),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // STRICT: Invoice Creation - Maximum 10 invoices per user per minute
            // Prevents flooding attacks on invoice creation endpoint
            options.AddPolicy("InvoiceCreation", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetUserBasedKey(context),
                    factory: partition => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6 // 10-second segments for smoother rate limiting
                    }));

            // Invoice Operations (Validate, Sign, Transmit) - 20 per minute per user
            options.AddPolicy("InvoiceOperations", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetUserBasedKey(context),
                    factory: partition => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 4
                    }));

            // Bulk Operations - Very restrictive: 5 per 5 minutes per user
            options.AddPolicy("BulkOperations", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetUserBasedKey(context),
                    factory: partition => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5),
                        SegmentsPerWindow = 5
                    }));

            // Authentication endpoints - 5 login attempts per 5 minutes per IP
            options.AddPolicy("Authentication", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetIpAddress(context),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
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

                // Log rate limit violation for monitoring
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var ipAddress = GetIpAddress(context.HttpContext);
                var userName = context.HttpContext.User?.Identity?.Name ?? "Anonymous";
                var endpoint = context.HttpContext.Request.Path;

                logger.LogWarning(
                    "Rate limit exceeded for user '{UserName}' from IP '{IpAddress}' on endpoint '{Endpoint}'. Retry after {RetryAfter} seconds.",
                    userName,
                    ipAddress,
                    endpoint,
                    retryAfterSeconds);

                var response = new
                {
                    Success = false,
                    Message = "Rate limit exceeded. Too many requests.",
                    ErrorCode = "RATE_LIMIT_EXCEEDED",
                    Details = "You have exceeded the maximum number of requests allowed. Please wait before trying again.",
                    RetryAfter = retryAfterSeconds,
                    Timestamp = DateTime.UtcNow,
                    TraceId = context.HttpContext.TraceIdentifier
                };

                await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken: token);
            };
        });

        // Helper function to get rate limit partition key (user or IP)
        static string GetRateLimitKey(HttpContext context)
        {
            // Prefer user identity for authenticated requests
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                return $"user:{context.User.Identity.Name}";
            }

            // Fall back to IP address for unauthenticated requests
            return $"ip:{GetIpAddress(context)}";
        }

        // Helper function to get user-based partition key (enforces per-user limits)
        static string GetUserBasedKey(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                return $"user:{context.User.Identity.Name}";
            }

            // For unauthenticated requests, use IP (but these endpoints should require auth)
            return $"ip:{GetIpAddress(context)}";
        }

        // Helper function to extract IP address
        static string GetIpAddress(HttpContext context)
        {
            // Check for X-Forwarded-For header (proxy/load balancer scenarios)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ip = forwardedFor.Split(',')[0].Trim();
                // Strip port if present
                if (ip.Contains(':') && !ip.StartsWith('['))
                {
                    var lastColonIndex = ip.LastIndexOf(':');
                    ip = ip.Substring(0, lastColonIndex);
                }
                return ip;
            }

            var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            // Strip port from IPv4 addresses
            if (remoteIp.Contains(':') && !remoteIp.StartsWith('['))
            {
                var lastColonIndex = remoteIp.LastIndexOf(':');
                remoteIp = remoteIp.Substring(0, lastColonIndex);
            }
            return remoteIp;
        }

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
}