using AspNetCore.Totp;
using AspNetCore.Totp.Interface;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Services;
using AegisEInvoicing.Infrastructure.BackgroundServices;
using AegisEInvoicing.Infrastructure.Services;
using AegisEInvoicing.Infrastructure.Services.Caching;
using AegisEInvoicing.Infrastructure.Services.EventBus;
using AegisEInvoicing.Infrastructure.Services.FirsMbs;
using AegisEInvoicing.Infrastructure.Services.Implementation;
using AegisEInvoicing.Infrastructure.Services.Session;
using AegisEInvoicing.Infrastructure.Services.Security;
using AegisEInvoicing.Infrastructure.Services.Licensing;
using AegisEInvoicing.Infrastructure.Services.Telemetry;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using AegisEInvoicing.Infrastructure.Services.Interfaces;

namespace AegisEInvoicing.Infrastructure;

/// <summary>
/// Infrastructure dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DateTime service
        services.AddSingleton<IDateTime, DateTimeService>();

        // Current user service
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Invoice approval service
        services.AddScoped<IInvoiceApprovalService, InvoiceApprovalService>();

        // JWT token service
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Token blacklist service for session replay attack prevention
        services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

        // Session management service for concurrent session control
        services.Configure<SessionManagementSettings>(configuration.GetSection(SessionManagementSettings.SectionName));
        services.AddScoped<ISessionManagementService, SessionManagementService>();

        // Response tampering protection services
        services.AddSingleton<IResponseIntegrityService, ResponseIntegrityService>();
        services.AddScoped<IInvoiceAuditService, InvoiceAuditService>();

        // Telemetry Service via SigNoz (OTLP)
        services.AddScoped<ITelemetryService, SigNozTelemetryService>();

        // Reference Data Cache Service (validates invoice types, currencies, etc.)
        services.AddSingleton<IReferenceDataCacheService, ReferenceDataCacheService>();

        // FIRS API configuration services
        services.AddScoped<IFIRSApiKeyService, FIRSApiKeyService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();

        // FIRS MBS portal client
        services.AddHttpClient<IFirsMbsApiClient, FirsMbsApiClient>();

        // Licensing Service (On-Premise deployments)
        services.Configure<LicensingServiceOptions>(configuration.GetSection(LicensingServiceOptions.SectionName));
        services.AddHttpClient<ILicensingService, LicensingService>();

        //OTP Generator
        services.AddTransient<ITotpGenerator, TotpGenerator>();
        services.AddSingleton<ITotpService, TotpService>();

        // Webhook notification service
        // services.AddHttpClient<IWebhookNotificationService, WebhookNotificationService>();

        // Invoice transmission queue service
        services.AddScoped<IInvoiceTransmissionQueueService, InvoiceTransmissionQueueService>();
        services.AddScoped<IFIRSCurrencyValidationService, FIRSCurrencyValidationService>();

        // Caching
        ConfigureCache(services, configuration);

        // Event Bus
        ConfigureEventBus(services, configuration);

        // License and Subscription Services
        services.AddScoped<ILicenseValidationService, LicenseValidationService>();



        // SFTP Directory Management Service
        services.AddScoped<ISftpDirectoryService, SftpDirectoryService>();

        // APP Provider routing — AppProviderRouter discovers adapters by ProviderCode.
        // Adapters self-register in their respective integration projects.
        services.AddScoped<IAppProviderRouter, AppProviderRouter>();

        // Background Services
        services.AddHostedService<OutboxProcessorService>();
        services.AddHostedService<FIRSConfigurationInitializationService>();
        services.AddHostedService<SubscriptionMonitorService>();
        services.AddHostedService<ReceivedInvoicesSyncBackgroundService>();
        services.AddHostedService<ReferenceDataRefreshBackgroundService>();
        //services.AddHostedService<InvoiceValidationBackgroundService>();
        //services.AddHostedService<InvoiceSigningBackgroundService>();
        //services.AddHostedService<InvoiceTransmissionBackgroundService>();


        // External Services
        ConfigureExternalServices(services, configuration);

        return services;
    }

    private static void ConfigureCache(IServiceCollection services, IConfiguration configuration)
    {
        if (configuration != null)
        {
            services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        }
        else
        {
            services.Configure<CacheOptions>(options => { });
        }

        // Add memory cache first (always available)
        services.AddMemoryCache();

        var redisConnection = configuration?.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            try
            {
                var redis = ConnectionMultiplexer.Connect(redisConnection);
                services.AddSingleton<IConnectionMultiplexer>(redis);
                services.AddStackExchangeRedisCache(options =>
                {
                    options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(redis);
                });
                Console.WriteLine("Successfully connected to Redis for distributed caching");
            }
            catch (Exception ex)
            {
                // Log but don't fail startup - fallback to memory cache
                Console.WriteLine($"Failed to connect to Redis: {ex.Message}. Falling back to in-memory distributed cache.");
                // Register null connection so RedisCacheService can fallback to memory cache
                services.AddSingleton<IConnectionMultiplexer>(provider => null!);
                // Use in-memory distributed cache as fallback
                services.AddDistributedMemoryCache();
            }
        }
        else
        {
            // Register null connection when Redis not configured
            Console.WriteLine("Redis connection string not configured. Using in-memory distributed cache.");
            services.AddSingleton<IConnectionMultiplexer>(provider => null!);
            // Use in-memory distributed cache as fallback
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheService, RedisCacheService>();
    }

    private static void ConfigureEventBus(IServiceCollection services, IConfiguration configuration)
    {
        if (configuration != null)
        {
            services.Configure<EventBusSettings>(configuration.GetSection(EventBusSettings.SectionName));
        }
        else
        {
            services.Configure<EventBusSettings>(options => { });
        }

        var rabbitMqConnection = configuration?.GetConnectionString("RabbitMQ");
        if (!string.IsNullOrEmpty(rabbitMqConnection))
        {
            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitMqConnection));
                    cfg.ConfigureEndpoints(context);
                });
            });
        }
        else
        {
            // Register null IBus when RabbitMQ not configured
            services.AddSingleton<IBus>(provider => null!);
        }

        services.AddScoped<IEventBus, ResilientEventBus>();
    }

    private static void ConfigureExternalServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configure IntegrationService options
        if (configuration != null)
        {
            services.Configure<IntegrationServiceOptions>(configuration.GetSection(IntegrationServiceOptions.SectionName));
        }
        else
        {
            services.Configure<IntegrationServiceOptions>(options => { });
        }

        // HTTP clients - resilience policies are now handled in the service itself
        services.AddHttpClient<IIntegrationService, IntegrationService>(client =>
        {
            client.BaseAddress = new Uri(configuration?["FIRSConfiguration:BaseUrl"] ?? "https://api.example.com");

            // Add standard headers that Postman likely includes
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "AegisEInvoicing/1.0");

            // Ensure Content-Type is set for JSON (this is usually automatic with StringContent, but being explicit)
            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");

            // Set default timeout
            client.Timeout = TimeSpan.FromSeconds(60);
        });
    }



}
