using DotNetEnv;
using AegisEInvoicing.Application;
using AegisEInvoicing.FIRSAccessPoint;
using AegisEInvoicing.Infrastructure;
using AegisEInvoicing.Interswitch;
using AegisEInvoicing.NotificationService;
using AegisEInvoicing.Persistence;
using AegisEInvoicing.ERP.API.Authentication;
using AegisEInvoicing.ERP.API.Extensions;
using AegisEInvoicing.ERP.API.Middleware;
using Microsoft.AspNetCore.Authentication;
using QuestPDF.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using AegisEInvoicing.Infrastructure.Services.Telemetry;

namespace AegisEInvoicing.ERP.API;

public class Program
{
    public static void Main(string[] args)
    {
        // Load environment variables from .env file
        // Try to load from project directory first, then current directory
        var projectDir = Directory.GetCurrentDirectory();
        var envPath = Path.Combine(projectDir, ".env");

        if (File.Exists(envPath))
        {
            Env.Load(envPath);
            Console.WriteLine($"Loaded .env from: {envPath}");
        }
        else
        {
            Console.WriteLine($"Warning: .env file not found at {envPath}");
            Env.Load(); // Try loading from current directory as fallback
        }

        // Verify critical environment variables are loaded
        var jwtSecret = Environment.GetEnvironmentVariable("Jwt__SecretKey");
        var dbConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        Console.WriteLine($"Jwt__SecretKey loaded: {!string.IsNullOrEmpty(jwtSecret)}");
        Console.WriteLine($"ConnectionStrings__DefaultConnection loaded: {!string.IsNullOrEmpty(dbConnection)}");

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/saas-api-.txt", rollingInterval: RollingInterval.Day)
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting EInvoice Integrator SaaS API");

            var builder = WebApplication.CreateBuilder(args);

            // Add environment variables to configuration
            builder.Configuration.AddEnvironmentVariables();

            // Add Application Insights Telemetry
            var appInsightsConnectionString = Environment.GetEnvironmentVariable("ApplicationInsights__ConnectionString");
            if (!string.IsNullOrEmpty(appInsightsConnectionString))
            {
                builder.Services.AddApplicationInsightsTelemetry(options =>
                {
                    options.ConnectionString = appInsightsConnectionString;
                    options.EnableDependencyTrackingTelemetryModule = bool.Parse(Environment.GetEnvironmentVariable("ApplicationInsights__EnableDependencyTracking") ?? "true");
                    options.EnablePerformanceCounterCollectionModule = bool.Parse(Environment.GetEnvironmentVariable("ApplicationInsights__EnablePerformanceCounters") ?? "true");
                    options.EnableRequestTrackingTelemetryModule = bool.Parse(Environment.GetEnvironmentVariable("ApplicationInsights__EnableRequestTracking") ?? "true");
                });

                // Add custom telemetry initializer
                var cloudRoleName = Environment.GetEnvironmentVariable("ApplicationInsights__CloudRoleName") ?? "AegisEInvoicing-SaaS";
                builder.Services.AddSingleton(new CustomTelemetryInitializer(cloudRoleName, "1.0.0"));

                Log.Information("Application Insights telemetry enabled with connection string");
            }
            else
            {
                Log.Warning("Application Insights connection string not configured - telemetry will be limited to logs only");
            }

            // Add Serilog
            builder.Host.UseSerilog((context, services, configuration) =>
            {
                // Get Application Insights configuration from environment variables
                var appInsightsConnectionString = Environment.GetEnvironmentVariable("ApplicationInsights__ConnectionString");
                var cloudRoleName = Environment.GetEnvironmentVariable("ApplicationInsights__CloudRoleName") ?? "AegisEInvoicing-SaaS";

                var environmentName = context.HostingEnvironment.EnvironmentName?.ToLowerInvariant().Replace(".", "-") ?? "unknown";

                Log.Information("=== Application Insights Configuration ===");
                Log.Information("Environment: {Environment}", environmentName);
                Log.Information("Cloud Role Name: {CloudRoleName}", cloudRoleName);
                Log.Information("Has Connection String: {HasConnectionString}", !string.IsNullOrEmpty(appInsightsConnectionString));

                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.WithProperty("Application", cloudRoleName)
                    .Enrich.WithProperty("Environment", environmentName)
                    .Enrich.WithMachineName()
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(
                        "logs/saas-api-.txt",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

                // Configure Application Insights if connection string is available
                if (!string.IsNullOrEmpty(appInsightsConnectionString))
                {
                    try
                    {
                        configuration.WriteTo.ApplicationInsights(
                            appInsightsConnectionString,
                            TelemetryConverter.Traces);

                        Log.Information("Application Insights configured successfully");
                        Log.Information("Cloud Role: {CloudRoleName}", cloudRoleName);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to configure Application Insights - logging will continue to Console and File only");
                    }
                }
                else
                {
                    Log.Warning("Application Insights connection string not configured - skipping Application Insights sink");
                    Log.Warning("Logs will only be written to Console and File");
                    Log.Warning("Check .env file for: ApplicationInsights__ConnectionString");
                }
            });

            // Add exception handling
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();


            // Add services to the container
            builder.Services.AddHttpContextAccessor(); // Required for CurrentUserService
            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructureServices(builder.Configuration);
            builder.Services.AddPersistenceServices(builder.Configuration);
            builder.Services.AddFIRSAccessPoint(builder.Configuration);
            builder.Services.AddInterswitchIntegration(builder.Configuration);

            // Email service (used by various handlers)
            builder.Services.AddEmailService(builder.Configuration);

            // Map the Application's IIntegrationService to FIRS's IIntegrationService interface using adapter
            builder.Services.AddScoped<AegisEInvoicing.FIRSAccessPoint.Interfaces.IIntegrationService, AegisEInvoicing.ERP.API.Services.IntegrationServiceAdapter>();

            // Add API services (includes API versioning, controllers, swagger, etc.)
            builder.Services.AddApiServices(builder.Configuration);

            // Configure API key authentication as the only authentication method
            builder.Services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = ApiKeyAuthenticationOptions.DefaultScheme;
                options.DefaultAuthenticateScheme = ApiKeyAuthenticationOptions.DefaultScheme;
                options.DefaultChallengeScheme = ApiKeyAuthenticationOptions.DefaultScheme;
            });

            builder.Services.AddScoped(typeof(Microsoft.AspNetCore.Identity.IPasswordHasher<>), typeof(Microsoft.AspNetCore.Identity.PasswordHasher<>));
            builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationOptions.DefaultScheme, options =>
            {
                options.HeaderName = "X-API-Key";
                options.QueryStringKey = "api_key";
            });

            QuestPDF.Settings.License = LicenseType.Community;

            // Configure Kestrel to remove Server header for security
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.AddServerHeader = false;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline

            // Handle forwarded headers from reverse proxies (required for hosting behind load balancers/proxies)
            // This must be FIRST in the pipeline to correctly set scheme, host, and client IP
            //app.UseForwardedHeaders(new ForwardedHeadersOptions
            //{
            //    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
            //});

            // Exception handler must be first after forwarded headers
            app.UseExceptionHandler();

            // API Header Validation - Validate required headers before any processing
            // Returns clear error messages for missing X-API-Key, invalid Content-Type, etc.
            app.UseApiHeaderValidation();

            // Add HTTP method restriction middleware (after security headers)
            app.UseHttpMethodRestriction();

            // Add sensitive data in URL protection middleware (after HTTP method restriction)
            // Addresses VAPT finding: Password submitted using GET method
            app.UseSensitiveDataProtection();

            app.UseSerilogRequestLogging();

            var enforceHttps = builder.Configuration.GetValue<bool>("Security:EnforceHttps", false);

            if (enforceHttps)
            {
                // Add security headers middleware (should be first after exception handler)
                app.UseSecurityHeaders();

                // HSTS (HTTP Strict Transport Security)
                // Enable HSTS to enforce HTTPS
                // Addresses VAPT finding: Unencrypted communications
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

            // Scalar API documentation (replaces Swagger)
            app.MapOpenApi("/openapi/v1.json").WithGroupName("v1");

            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("EInvoice Integrator SaaS API Documentation")
                    .WithTheme(ScalarTheme.Purple)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });

            app.UseStaticFiles();

            app.UseRouting();
            // CORS - Use named policy with secure configuration
            // Addresses VAPT finding: Insecure CORS
            app.UseCors("AllowSpecificOrigins");

            // Origin validation middleware (before authentication)
            app.UseOriginValidation();
            app.UseEndpointRateLimit();
            app.UseIdempotency();
            app.UseReplayProtection();
            app.UseResponseIntegrity();

            app.UseAuthentication();
            app.UseAuthorization();

            // Add custom middleware
            app.UseMiddleware<ApiUsageTrackingMiddleware>();
            // Add rate limiting
            app.UseRateLimiter();

            app.MapControllers();
            //.RequireRateLimiting("ApiOnly");

            // Health check endpoint
            app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}