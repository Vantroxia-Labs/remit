using AspNetCore.Totp;
using AspNetCore.Totp.Interface;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Services;
using AegisEInvoicing.Infrastructure.Services;
using AegisEInvoicing.Infrastructure.Services.Caching;
using AegisEInvoicing.Infrastructure.Services.EventBus;
using AegisEInvoicing.Infrastructure.Services.FirsMbs;
using AegisEInvoicing.Infrastructure.Services.Implementation;
using AegisEInvoicing.Infrastructure.Services.Licensing;
using AegisEInvoicing.Infrastructure.Services.Session;
using AegisEInvoicing.Infrastructure.Services.Interfaces;
using AegisEInvoicing.Infrastructure.Services.Telemetry;
using MassTransit;
using StackExchange.Redis;

namespace AegisEInvoicing.SFTP.API.Extensions
{
    /// <summary>
    /// Custom infrastructure services registration that excludes background services from Infrastructure layer
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds infrastructure services without the infrastructure background services
        /// </summary>
        public static IServiceCollection AddInfrastructureServicesWithoutBackgroundServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // DateTime service
            services.AddSingleton<IDateTime, DateTimeService>();

            // Current user service
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // JWT token service
            services.AddScoped<IJwtTokenService, JwtTokenService>();

            // Token blacklist service for session replay attack prevention
            services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

            // FIRS API configuration services
            services.AddScoped<IFIRSApiKeyService, FIRSApiKeyService>();
            services.AddSingleton<IEncryptionService, EncryptionService>();

            // Reference Data Cache Service (validates invoice types, currencies, etc.)
            services.AddSingleton<IReferenceDataCacheService, ReferenceDataCacheService>();

            // Application Insights Telemetry Service (nullable TelemetryClient support)
            services.AddScoped<ITelemetryService, ApplicationInsightsTelemetryService>();

            // FIRS MBS portal client
            services.AddHttpClient<IFirsMbsApiClient, FirsMbsApiClient>();

            // Licensing Service (On-Premise deployments)
            services.Configure<LicensingServiceOptions>(configuration.GetSection(LicensingServiceOptions.SectionName));
            services.AddHttpClient<ILicensingService, LicensingService>();

            //OTP Generator
            services.AddTransient<ITotpGenerator, TotpGenerator>();
            services.AddSingleton<ITotpService, TotpService>();

            // Invoice transmission queue service
            services.AddScoped<IInvoiceTransmissionQueueService, InvoiceTransmissionQueueService>();

            // Caching
            ConfigureCache(services, configuration);

            // Event Bus
            ConfigureEventBus(services, configuration);

            // License and Subscription Services
            services.AddScoped<ILicenseValidationService, LicenseValidationService>();

            // Invoice approval / rejection service
            services.AddScoped<IInvoiceApprovalService, InvoiceApprovalService>();

            // Session management service (used by Login handlers)
            services.AddScoped<ISessionManagementService, SessionManagementService>();

            // SFTP Directory Management Service
            services.AddScoped<ISftpDirectoryService, SftpDirectoryService>();

            // NOTE: Background Services are EXCLUDED here
            // The following services are NOT added:
            // - OutboxProcessorService
            // - FIRSConfigurationInitializationService
            // - SubscriptionMonitorService
            // - InvoiceValidationBackgroundService
            // - InvoiceSigningBackgroundService
            // - InvoiceTransmissionBackgroundService

            // External Services
            ConfigureExternalServices(services, configuration);

            // Reference Data Cache Service (validates invoice types, currencies, etc.)
            services.AddSingleton<IReferenceDataCacheService, ReferenceDataCacheService>();
            // License and Subscription Services
            services.AddScoped<ILicenseValidationService, LicenseValidationService>();

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
                }
                catch (Exception ex)
                {
                    // Log but don't fail startup - fallback to in-memory distributed cache
                    Console.WriteLine($"Failed to connect to Redis: {ex.Message}. Falling back to in-memory distributed cache.");
                    services.AddSingleton<IConnectionMultiplexer>(provider => null!);
                    services.AddDistributedMemoryCache();
                }
            }
            else
            {
                // Redis not configured - use in-memory distributed cache
                services.AddSingleton<IConnectionMultiplexer>(provider => null!);
                services.AddDistributedMemoryCache();
            }

            services.AddMemoryCache();
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
                client.BaseAddress = new Uri(configuration?["FIRSApiConfiguration:DefaultConfiguration:BaseUrl"] ?? "https://api.example.com");

                // Only add API key if configured (avoid null values)
                var apiKey = configuration?["FIRSApiConfiguration:DefaultConfiguration:ApiKey"];
                var apiSecret = configuration?["FIRSApiConfiguration:DefaultConfiguration:ApiSecret"];

                if (!string.IsNullOrEmpty(apiKey))
                    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                if (!string.IsNullOrEmpty(apiSecret))
                    client.DefaultRequestHeaders.Add("X-API-Secret", apiSecret);

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
}