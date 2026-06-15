using DotNetEnv;
using AegisEInvoicing.Portal.API.Extensions;
using AegisEInvoicing.Portal.API.Middleware;
using AegisEInvoicing.Application;
using AegisEInvoicing.FIRSAccessPoint;
using AegisEInvoicing.Infrastructure;
using AegisEInvoicing.Infrastructure.Services.Telemetry;
using AegisEInvoicing.Interswitch;
using AegisEInvoicing.NotificationService;
using AegisEInvoicing.Paystack;
using AegisEInvoicing.Persistence;
using Microsoft.AspNetCore.Http.Features;
using OfficeOpenXml;
using QuestPDF.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

// CRITICAL: Write to both Console and Debug to ensure visibility in Azure
var startupMessage = $"[STARTUP {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Application starting - Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"}";
Console.WriteLine(startupMessage);
System.Diagnostics.Debug.WriteLine(startupMessage);

// Write to a file that Azure can read (in case console isn't captured)
try
{
    var logPath = Path.Combine(Directory.GetCurrentDirectory(), "startup-debug.log");
    File.AppendAllText(logPath, $"{startupMessage}\n");
    Console.WriteLine($"[STARTUP] Debug log written to: {logPath}");
}
catch (Exception ex)
{
    Console.WriteLine($"[STARTUP ERROR] Could not write debug log: {ex.Message}");
}

// Load environment variables from .env file
// Try to load from project directory first, then current directory
var projectDir = Directory.GetCurrentDirectory();
var envPath = Path.Combine(projectDir, ".env");

if (File.Exists(envPath))
{
    Env.Load(envPath);
    Console.WriteLine($"[ENV LOAD] SUCCESS: Loaded .env from: {envPath}");

    // Log a sample CORS config to verify it's loaded
    var sampleCors = Environment.GetEnvironmentVariable("Cors__AllowedOrigins__5");
    Console.WriteLine($"[ENV CHECK] Cors__AllowedOrigins__5 = {sampleCors ?? "NOT SET"}");
}
else
{
    Console.WriteLine($"[ENV LOAD] WARNING: .env file not found at {envPath}");
    Env.Load(); // Try loading from current directory as fallback
    Console.WriteLine($"[ENV LOAD] Attempted fallback load from current directory");
}

// Verify critical environment variables are loaded
var encryptionKey = Environment.GetEnvironmentVariable("Encryption__Key");
var encryptionIv = Environment.GetEnvironmentVariable("Encryption__Iv");
Console.WriteLine($"Encryption__Key loaded: {!string.IsNullOrEmpty(encryptionKey)}");
Console.WriteLine($"Encryption__Iv loaded: {!string.IsNullOrEmpty(encryptionIv)}");

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting EInvoice Integrator API");

    var builder = WebApplication.CreateBuilder(args);

    // Add environment variables to configuration
    builder.Configuration.AddEnvironmentVariables();

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();

    // Add Serilog
    builder.Host.UseSerilog((context, services, loggerConfig) =>
    {
        loggerConfig
            // ===============================
            // CONFIGURATION SOURCES
            // ===============================
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)

            // ===============================
            // MINIMUM LEVEL OVERRIDES
            // ===============================
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Query", LogEventLevel.Debug)

            // ===============================
            // ENRICHERS
            // ===============================
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentUserName()
            .Enrich.WithProperty("Application", "AegisEInvoicing.Portal.API")
            .Enrich.WithProperty(
                "Environment",
                context.HostingEnvironment.EnvironmentName
            )

            // ===============================
            // SINKS
            // ===============================

            // REQUIRED for Azure Log Stream
            .WriteTo.Console()

            // Azure-safe file logging (optional, warnings+ only)
            .WriteTo.File(
                path: "/home/LogFiles/aegiseinvoicing-.log",
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Warning,
                shared: true,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] " +
                    "({MachineName}) {Application} {Environment} " +
                    "{Message:lj}{NewLine}{Exception}"
            );


    });


    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Add services to the container
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddPersistenceServices(builder.Configuration);
    builder.Services.AddFIRSAccessPoint(builder.Configuration);
    builder.Services.AddInterswitchIntegration(builder.Configuration);
    builder.Services.AddEmailService(builder.Configuration);
    builder.Services.AddPaystackIntegration(builder.Configuration);

    // =================================================================
    // CORS CONFIGURATION
    // =================================================================
    // Addresses VAPT finding: Insecure CORS
    // Ensures only trusted origins can access the API with credentials
    // IMPORTANT: Never use wildcard "*" with AllowCredentials
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var corsConfig = builder.Configuration.GetSection("Cors");

            // Get allowed origins from configuration
            // SECURITY: No wildcard fallback - fail-secure if not configured
            var allowedOrigins = corsConfig.GetSection("AllowedOrigins").Get<string[]>();

            var isDevelopment = builder.Environment.IsDevelopment();

            // If no origins configured, throw exception (fail-secure approach)
            if (allowedOrigins == null || allowedOrigins.Length == 0)
            {
                if (isDevelopment)
                {
                    allowedOrigins = ["http://localhost:5173", "http://localhost:5174"];
                    Console.WriteLine("[CORS CONFIG] Using development localhost defaults because Cors:AllowedOrigins was empty.");
                }
                else
                {
                    throw new InvalidOperationException(
                        "CORS configuration error: No allowed origins specified in configuration. " +
                        "Please configure 'Cors:AllowedOrigins' in appsettings.json or .env file. " +
                        "Never use wildcard '*' with AllowCredentials.");
                }
            }

            // Validate that wildcard is not used with credentials
            if (allowedOrigins.Contains("*"))
            {
                throw new InvalidOperationException(
                    "CORS security violation: Wildcard '*' origin is not allowed with credentials. " +
                    "Please specify explicit trusted origins in 'Cors:AllowedOrigins' configuration.");
            }

            // Get allowed methods (secure defaults - no OPTIONS to prevent method enumeration)
            var allowedMethods = corsConfig.GetSection("AllowedMethods").Get<string[]>()
                ?? ["GET", "POST", "PUT", "DELETE", "PATCH"];

            // Get allowed headers (specific headers only - no wildcard)
            var allowedHeaders = corsConfig.GetSection("AllowedHeaders").Get<string[]>()
                ?? ["Content-Type", "Authorization", "X-API-Version", "X-Request-ID"];

            // Get credentials setting (default: false for security)
            var allowCredentials = corsConfig.GetValue<bool>("AllowCredentials", false);

            policy.WithOrigins(allowedOrigins)
                  .WithMethods(allowedMethods)
                  .WithHeaders(allowedHeaders)
                  .WithExposedHeaders("Content-Type", "Authorization", "X-Encrypted"); // Expose headers to browser

            if (allowCredentials)
            {
                policy.AllowCredentials();
            }

            // Log CORS configuration for security audit (both environments)
            var corsLogMessage = $"CORS configured with {allowedOrigins.Length} allowed origin(s): " +
                                $"[{string.Join(", ", allowedOrigins)}], " +
                                $"Methods: [{string.Join(", ", allowedMethods)}], " +
                                $"Headers: [{string.Join(", ", allowedHeaders)}], " +
                                $"AllowCredentials: {allowCredentials}";

            // Log to both console and Serilog for visibility in all environments
            Console.WriteLine($"[CORS CONFIG] {corsLogMessage}");
            Log.Information(corsLogMessage);

            // CRITICAL: Verify X-Encrypted is in the headers list
            if (!allowedHeaders.Contains("X-Encrypted", StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine("[CORS ERROR] X-Encrypted header is NOT in the allowed headers list!");
                Console.WriteLine($"[CORS ERROR] Current headers: [{string.Join(", ", allowedHeaders)}]");
                Console.WriteLine("[CORS ERROR] Adding X-Encrypted manually to fix CORS issue");

                // WORKAROUND: Add X-Encrypted to the headers list to fix CORS
                // This indicates appsettings.json is not being loaded correctly
                var headersList = allowedHeaders.ToList();
                headersList.Add("X-Encrypted");
                allowedHeaders = headersList.ToArray();

                // Reconfigure policy with the fixed headers
                policy.WithHeaders(allowedHeaders);

                Console.WriteLine($"[CORS FIX] Updated headers: [{string.Join(", ", allowedHeaders)}]");
            }
            else
            {
                Console.WriteLine("[CORS SUCCESS] X-Encrypted header is correctly configured in allowed headers");
            }
        });
    });

    // Map the Application's IIntegrationService to FIRS's IIntegrationService interface using adapter
    builder.Services.AddScoped<AegisEInvoicing.FIRSAccessPoint.Interfaces.IIntegrationService, AegisEInvoicing.Portal.API.Services.IntegrationServiceAdapter>();

    builder.Services.AddApiServices(builder.Configuration);

    // Add OpenTelemetry
    builder.Services.AddOpenTelemetryServices(builder.Configuration);
    builder.Services.AddScoped(typeof(Microsoft.AspNetCore.Identity.IPasswordHasher<>), typeof(Microsoft.AspNetCore.Identity.PasswordHasher<>));

    // Add session cleanup background service
    builder.Services.AddHostedService<AegisEInvoicing.Portal.API.BackgroundServices.SessionCleanupService>();
    builder.Services.AddHostedService<AegisEInvoicing.Portal.API.BackgroundServices.NrsWindowWarningService>();

    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 500_000_000; // 500MB
    });

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 500_000_000; // 500MB
        options.AddServerHeader = false; // Remove Server header for security
    });

    QuestPDF.Settings.License = LicenseType.Community;

    var app = builder.Build();

    // Configure the HTTP request pipeline
    app.UseExceptionHandler();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "Unknown");
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString() ?? "Unknown");
        };
    });

    // HSTS (HTTP Strict Transport Security)
    // Enable HSTS in both Development and Production to enforce HTTPS
    // Addresses VAPT finding: Unencrypted communications
    var enforceHttps = builder.Configuration.GetValue<bool>("Security:EnforceHttps", false);

    if (enforceHttps)
    {
        // Add security headers middleware (after CORS)
        app.UseSecurityHeaders();

        // HSTS (HTTP Strict Transport Security)
        app.UseHsts();

        // HTTPS Redirection - Automatically redirect HTTP to HTTPS
        app.UseHttpsRedirection();

        // HTTPS Enforcement - Additional middleware to enforce HTTPS and log violations
        app.UseHttpsEnforcement();

        Log.Information("HTTPS enforcement is ENABLED");
    }
    else
    {
        Log.Warning("HTTPS enforcement is DISABLED - This should only be used for testing without SSL certificates");
    }

    if (app.Environment.IsDevelopment())
    {
        //app.UseDeveloperExceptionPage();
    }

    // Scalar API documentation (replaces Swagger)
    app.MapOpenApi("/openapi/v1.json").WithGroupName("v1");
    app.MapOpenApi("/openapi/v2.json").WithGroupName("v2");

    // Apply OpenAPI version middleware to ensure IBM API Connect compatibility
    app.UseMiddleware<OpenApiVersionMiddleware>();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("AegisRemit E-Invoice Integrator API Documentation")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    app.UseStaticFiles();
    app.UseRouting();

    // =================================================================
    // CRITICAL: CORS must be AFTER UseRouting() and BEFORE UseAuthentication()
    // =================================================================
    // According to Microsoft docs, CORS must be after UseRouting() for endpoint routing
    // This is the correct position for CORS in ASP.NET Core 6+
    app.UseCors();

    // CORS Debugging Middleware - Log OPTIONS requests and CORS headers
    app.Use(async (context, next) =>
    {
        if (context.Request.Method == "OPTIONS")
        {
            Console.WriteLine($"[CORS DEBUG] OPTIONS request from Origin: {context.Request.Headers["Origin"]}");
            Console.WriteLine($"[CORS DEBUG] Requested Headers: {context.Request.Headers["Access-Control-Request-Headers"]}");
            Console.WriteLine($"[CORS DEBUG] Requested Method: {context.Request.Headers["Access-Control-Request-Method"]}");
        }

        await next();

        if (context.Request.Method == "OPTIONS")
        {
            Console.WriteLine($"[CORS DEBUG] Response Status: {context.Response.StatusCode}");
            Console.WriteLine($"[CORS DEBUG] Access-Control-Allow-Origin: {context.Response.Headers["Access-Control-Allow-Origin"]}");
            Console.WriteLine($"[CORS DEBUG] Access-Control-Allow-Headers: {context.Response.Headers["Access-Control-Allow-Headers"]}");
            Console.WriteLine($"[CORS DEBUG] Access-Control-Allow-Methods: {context.Response.Headers["Access-Control-Allow-Methods"]}");
            Console.WriteLine($"[CORS DEBUG] Access-Control-Allow-Credentials: {context.Response.Headers["Access-Control-Allow-Credentials"]}");
        }
    });


    // Add payload decryption middleware for sensitive endpoints (login, change-password, etc.)
    app.UsePayloadDecryption();

    // Add subscription validation middleware
    app.UseMiddleware<SubscriptionValidationMiddleware>();

    // HTTP method restriction middleware (after CORS, blocks disallowed methods)
    app.UseHttpMethodRestriction();

    // Sensitive data in URL protection middleware (after HTTP method restriction)
    // Addresses VAPT finding: Password submitted using GET method
    app.UseSensitiveDataProtection();

    // Origin validation middleware (after CORS, before authentication)
    app.UseOriginValidation();

    // Replay protection middleware (after origin validation, before authentication)
    app.UseReplayProtection();

    app.UseAuthentication();
    app.UseAuthorization();

    // Track session activity and enforce session timeouts (after authentication)
    app.UseSessionActivityTracking();

    app.UseRateLimiter();
    app.UseRequestLocalization();
    app.UseResponseCompression();
    app.UseResponseCaching();

    app.MapControllers();
    app.MapHealthChecks();
    app.MapMetrics();

    // Migrate database if in development
    if (app.Environment.IsDevelopment())
    {
        await app.MigrateDatabaseAsync();
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    // CRITICAL: Ensure error is visible in Azure logs
    var errorMessage = $"[FATAL ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Application terminated unexpectedly: {ex.Message}\nStack Trace: {ex.StackTrace}";
    Console.WriteLine(errorMessage);
    System.Diagnostics.Debug.WriteLine(errorMessage);

    // Write to file for Azure diagnostics
    try
    {
        var errorLogPath = Path.Combine(Directory.GetCurrentDirectory(), "startup-error.log");
        File.WriteAllText(errorLogPath, $"{errorMessage}\n\nFull Exception:\n{ex}");
        Console.WriteLine($"[ERROR LOG] Written to: {errorLogPath}");
    }
    catch { }

    Log.Fatal(ex, "Application terminated unexpectedly");

    // Re-throw to ensure Azure sees the error
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class public so test projects can access it
public partial class Program { }