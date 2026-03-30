using AegisEInvoicing.Application;
using AegisEInvoicing.SFTP.API.Configuration;
using AegisEInvoicing.SFTP.API.Health;
using AegisEInvoicing.SFTP.API.Jobs;
using AegisEInvoicing.SFTP.API.Middleware;
using AegisEInvoicing.SFTP.API.Services;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using AegisEInvoicing.FIRSAccessPoint;
using AegisEInvoicing.NotificationService;
using AegisEInvoicing.Persistence;
using AegisEInvoicing.SFTP.API.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.Text.Json.Serialization;
using DotNetEnv;

// Load environment variables from .env file
Env.Load();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/Integrator-Background-Service-.txt", 
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 10_000_000,
        retainedFileCountLimit: 7)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting EInvoice Integrator Background Service");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Add Application Insights for Worker Service
    var appInsightsConnectionString = Environment.GetEnvironmentVariable("ApplicationInsights__ConnectionString");
    if (!string.IsNullOrEmpty(appInsightsConnectionString))
    {
        builder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
            options.ConnectionString = appInsightsConnectionString;
            options.EnableDependencyTrackingTelemetryModule = bool.Parse(Environment.GetEnvironmentVariable("ApplicationInsights__EnableDependencyTracking") ?? "true");
            options.EnablePerformanceCounterCollectionModule = bool.Parse(Environment.GetEnvironmentVariable("ApplicationInsights__EnablePerformanceCounters") ?? "true");
        });

        Log.Information("Application Insights telemetry enabled for Background Service");
    }
    
    // Use Serilog with Application Insights
    builder.Host.UseSerilog((context, services, loggerConfig) =>
    {
        loggerConfig
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "AegisEInvoicing-BackgroundService")
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
            .Enrich.WithMachineName()
            .WriteTo.Console()
            .WriteTo.File("logs/Integrator-Background-Service-.txt", 
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 10_000_000,
                retainedFileCountLimit: 7);

        // Application Insights sink
        if (!string.IsNullOrEmpty(appInsightsConnectionString))
        {
            loggerConfig.WriteTo.ApplicationInsights(
                appInsightsConnectionString,
                TelemetryConverter.Traces);
        }
    });
    
    // Configuration
    builder.Services.Configure<SftpConfiguration>(builder.Configuration.GetSection(SftpConfiguration.SectionName));
    builder.Services.Configure<ProcessingConfiguration>(builder.Configuration.GetSection(ProcessingConfiguration.SectionName));
    builder.Services.Configure<NotificationConfiguration>(builder.Configuration.GetSection(NotificationConfiguration.SectionName));
    
    // Add services to the container
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
    
    // Add API versioning support to fix routing constraints
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
            new Asp.Versioning.UrlSegmentApiVersionReader(),
            new Asp.Versioning.HeaderApiVersionReader("x-api-version"));
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // Add services to the container
    builder.Services.AddHttpContextAccessor(); // Required for CurrentUserService
    
    // Add core application and infrastructure services
    builder.Services.AddApplicationServices(); // This adds MediatR and other core services
    builder.Services.AddInfrastructureServicesWithoutBackgroundServices(builder.Configuration); // This adds ICurrentUserService without Infrastructure background services
    builder.Services.AddPersistenceServices(builder.Configuration);
    builder.Services.AddFIRSAccessPoint(builder.Configuration); // This adds FIRS integration services
    
    // Register Interswitch HTTP client for MediatR handlers without strict options validation
    builder.Services.Configure<AegisEInvoicing.Interswitch.Configuration.InterswitchHttpClientOptions>(
        builder.Configuration.GetSection(AegisEInvoicing.Interswitch.Configuration.InterswitchHttpClientOptions.SectionName));

    builder.Services.AddHttpClient<AegisEInvoicing.Interswitch.Interfaces.IInterswitchHttpClient, AegisEInvoicing.Interswitch.Services.InterswitchHttpClient>((serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<AegisEInvoicing.Interswitch.Configuration.InterswitchHttpClientOptions>>()
            .Value;

        // Fallback to a dummy base URL if not configured to avoid startup failures in background service
        var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
            ? "https://localhost/"
            : options.BaseUrl;

        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = options.RequestTimeout;
    });
    builder.Services.AddHttpClient("SftpGoAdmin", client =>
    {
        var apiUrl = builder.Configuration["SftpGo:AdminApiUrl"] ?? "http://localhost:8080";
        var apiKey = builder.Configuration["SftpGo:ApiKey"];

        client.BaseAddress = new Uri(apiUrl);
        if (!string.IsNullOrEmpty(apiKey))
        {
            client.DefaultRequestHeaders.Add("X-SFTPGO-API-KEY", apiKey);
        }
    });
    builder.Services.AddScoped<ISftpGoAdminService>(sp =>
    {
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = factory.CreateClient("SftpGoAdmin");
        var logger = sp.GetRequiredService<ILogger<SftpGoAdminService>>();
        return new SftpGoAdminService(httpClient, logger);
    });
    builder.Services.AddEmailService(builder.Configuration);

    // Map the Application's IIntegrationService to FIRS's IIntegrationService interface using adapter
    builder.Services.AddScoped<AegisEInvoicing.FIRSAccessPoint.Interfaces.IIntegrationService, AegisEInvoicing.SFTP.API.Services.IntegrationServiceAdapter>();


    // Background service core services - using LocalFileSystemSftpService for SFTPGo (same machine)
    builder.Services.AddScoped<ISftpService, LocalFileSystemSftpService>();
    builder.Services.AddScoped<IDatabaseSftpService, DatabaseSftpService>();
    builder.Services.AddScoped<IXmlDeserializationService, XmlDeserializationService>();
    builder.Services.AddScoped<IXmlResponseService, XmlResponseService>();
    builder.Services.AddScoped<IInvoiceNotificationService, InvoiceNotificationService>();
    builder.Services.AddScoped<IFileProcessingService, FileProcessingService>();

    // SFTP Authentication Service for SFTPGo integration
    builder.Services.AddScoped<IVirtualUserAuthenticationService, VirtualUserAuthenticationService>();
    builder.Services.AddScoped(typeof(Microsoft.AspNetCore.Identity.IPasswordHasher<>), typeof(Microsoft.AspNetCore.Identity.PasswordHasher<>));

    // Add SFTP File Processing Background Service as hosted service
    builder.Services.AddHostedService<SftpFileProcessingBackgroundService>();

    // Add Reference Data Cache Refresh Background Service (refreshes FIRS data daily)
    builder.Services.AddHostedService<AegisEInvoicing.Infrastructure.BackgroundServices.ReferenceDataRefreshBackgroundService>();

    // NOTE: Using SFTPGo (external standalone server) for SFTP functionality
    // SFTPGo authenticates via API endpoint: /api/sftp-auth/check-credentials
    // Background service reads files directly from filesystem (C:/ftproot/uploads)
    Log.Information("SFTP Server: Using SFTPGo with LocalFileSystemSftpService (direct file access)");
    
    
    
    // Health checks - temporarily commented out for testing
    builder.Services.AddHealthChecks()
        .AddCheck<SftpHealthCheck>("sftp", HealthStatus.Degraded, new[] { "sftp", "connectivity" });
       
       
    // =================================================================
    // CORS CONFIGURATION
    // =================================================================
    // Addresses VAPT finding: Cross-Domain Misconfiguration
    // SECURITY: Never use AllowAnyOrigin() - restricts to explicit origins only
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            // Get allowed origins from configuration
            // SECURITY: No wildcard - fail-secure if not configured
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

            // If no origins configured, use empty array (most restrictive - no CORS allowed)
            // For background services, typically only internal/trusted origins should be allowed
            if (allowedOrigins == null || allowedOrigins.Length == 0)
            {
                Log.Warning("CORS: No allowed origins configured. Using restrictive 'self' only policy.");
                // Don't allow any cross-origin requests if not configured
                policy.SetIsOriginAllowed(_ => false);
                return;
            }

            // Validate that wildcard is not used
            if (allowedOrigins.Contains("*"))
            {
                Log.Warning("CORS security warning: Wildcard '*' origin detected. Blocking for security.");
                policy.SetIsOriginAllowed(_ => false);
                return;
            }

            // Get allowed methods (secure defaults)
            var allowedMethods = builder.Configuration.GetSection("Cors:AllowedMethods").Get<string[]>()
                ?? new[] { "GET", "POST" }; // Background service typically only needs GET/POST

            // Get allowed headers (specific headers only)
            var allowedHeaders = builder.Configuration.GetSection("Cors:AllowedHeaders").Get<string[]>()
                ?? new[] { "Content-Type", "Authorization", "X-Request-ID" };

            policy.WithOrigins(allowedOrigins)
                  .WithMethods(allowedMethods)
                  .WithHeaders(allowedHeaders);

            Log.Information("CORS configured with {Count} allowed origin(s): [{Origins}]",
                allowedOrigins.Length, string.Join(", ", allowedOrigins));
        });
    });
    
    var app = builder.Build();

    // Configure the HTTP request pipeline
    // VAPT: Removed UseDeveloperExceptionPage to prevent Application Error Disclosure
    // Even in Development, we use custom exception handling to avoid leaking stack traces
    app.UseExceptionHandler("/error");

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }
    
    app.UseHttpsRedirection();

    // VAPT: Enforce HTTPS and block/redirect unencrypted HTTP requests
    app.UseHttpsEnforcement();

    // VAPT: Add security headers including CSP to all responses
    app.UseSecurityHeaders();

    app.UseCors();
    
    // Health checks endpoints
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                Status = report.Status.ToString(),
                Checks = report.Entries.Select(entry => new
                {
                    Name = entry.Key,
                    Status = entry.Value.Status.ToString(),
                    Description = entry.Value.Description,
                    Duration = entry.Value.Duration.TotalMilliseconds,
                    Data = entry.Value.Data
                }),
                TotalDuration = report.TotalDuration.TotalMilliseconds
            };
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));
        }
    });
    
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // No checks, just return if the app is responsive
    });
    
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("connectivity")
    });
    
    app.UseRouting();
    app.UseAuthorization();
    app.MapControllers();
    
    // Service status endpoint - temporarily commented out for testing
    app.MapGet("/status", async (IFileProcessingService fileProcessingService) =>
    {
        var status = await fileProcessingService.GetServiceStatusAsync();
        return Results.Ok(status);
    });
    
    // Manual trigger endpoint for testing
    app.MapPost("/trigger", async (IFileProcessingService fileProcessingService) =>
    {
        var statistics = await fileProcessingService.ProcessAllPendingFilesAsync();
        return Results.Ok(new { Message = "Processing triggered manually", Statistics = statistics });
    });
    
    // Diagnostic endpoints are now available via the controller routing
    
    Log.Information("EInvoice Integrator Background Service started successfully");
    Log.Information("Health check endpoint: /health");
    Log.Information("Service status endpoint: /status");
    Log.Information("Manual trigger endpoint: /trigger (POST)");
    Log.Information("Diagnostic endpoints: /api/diagnostics, /api/diagnostics/sftp, /api/diagnostics/quartz");
    Log.Information("Processing interval: {ProcessingInterval} seconds", builder.Configuration.GetValue<int>("Processing:ProcessingIntervalSeconds"));
    
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
