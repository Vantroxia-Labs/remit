using Amazon.SimpleEmail;
using AegisEInvoicing.NotificationService.Extensions;
using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;
using AegisEInvoicing.NotificationService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AegisEInvoicing.NotificationService;

public static class DependencyInjection
{
    /// <summary>
    /// Adds Azure Communication Service Email Service (DEFAULT)
    /// </summary>
    public static IServiceCollection AddAzureCommunicationEmailService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AzureCommunicationConfiguration>(options =>
        {
            var section = configuration.GetSection("AzureCommunicationService");
            options.ConnectionString = section["ConnectionString"] ?? "";
            options.DefaultFromEmail = section["DefaultFromEmail"] ?? "";
            options.DefaultFromName = section["DefaultFromName"] ?? "";
            options.MaxRetries = int.Parse(section["MaxRetries"] ?? "3");
            options.InitialRetryDelay = TimeSpan.FromSeconds(int.Parse(section["InitialRetryDelaySeconds"] ?? "1"));
            options.MaxRetryDelay = TimeSpan.FromSeconds(int.Parse(section["MaxRetryDelaySeconds"] ?? "30"));
            options.RequestTimeout = TimeSpan.FromSeconds(int.Parse(section["RequestTimeoutSeconds"] ?? "100"));
            options.MaxConcurrentOperations = int.Parse(section["MaxConcurrentOperations"] ?? "10");
            options.EnableTelemetry = bool.Parse(section["EnableTelemetry"] ?? "true");
        });

        services.AddScoped<IEmailService, AzureCommunicationEmailService>();

        return services;
    }

    /// <summary>
    /// Adds AWS SES Email Service using a connection string
    /// </summary>
    public static IServiceCollection AddAwsSesEmailService(
        this IServiceCollection services,
        string connectionString)
    {
        var config = AwsSesConfigurationExtensions.ParseConnectionString(connectionString);

        services.AddSingleton(Options.Create(config));

        services.AddSingleton<IAmazonSimpleEmailService>(serviceProvider =>
        {
            var awsConfig = new AmazonSimpleEmailServiceConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(config.Region),
                Timeout = config.TimeoutSeconds,
                MaxErrorRetry = config.MaxRetries
            };

            return new AmazonSimpleEmailServiceClient(config.AccessKey, config.SecretKey, awsConfig);
        });

        services.AddScoped<IEmailService, AwsSesEmailService>();

        return services;
    }

    public static IServiceCollection AddEmailService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EMAIL_SEND_ENABLED=false  → skip sending (dev default); true → use real provider
        var sendEnabled = !bool.TryParse(configuration["EmailSettings:SendEnabled"], out var enabled) || enabled;

        if (!sendEnabled)
        {
            services.AddScoped<IEmailService, _SkipEmailService>();
            return services;
        }

        var emailProvider = configuration["EmailSettings:Provider"]?.ToLower() ?? "azure";

        switch (emailProvider)
        {
            case "azure":
            case "azurecommunication":
                services.AddAzureCommunicationEmailService(configuration);
                break;

            case "aws":
            case "ses":
                var awsConnectionString = configuration["EmailSettings:AwsConnectionString"];
                if (string.IsNullOrWhiteSpace(awsConnectionString))
                    throw new InvalidOperationException("AWS SES connection string is not configured");

                services.AddAwsSesEmailService(awsConnectionString);
                break;

            default:
                services.AddAzureCommunicationEmailService(configuration);
                break;
        }

        return services;
    }

}

/// <summary>
/// Inline stub — used when EmailSettings:SendEnabled is false.
/// Defined here, not in a separate file. Logs only, nothing is dispatched.
/// </summary>
internal sealed class _SkipEmailService(ILogger<_SkipEmailService> logger) : IEmailService
{
    public Task<EmailResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[EMAIL DISABLED] To: {To} | Subject: {Subject}", message.To, message.Subject);
        return Task.FromResult(EmailResult.Success("skipped"));
    }

    public Task<EmailResult> SendBulkEmailAsync(List<EmailMessage> messages, CancellationToken cancellationToken = default)
    {
        foreach (var m in messages)
            logger.LogInformation("[EMAIL DISABLED] To: {To} | Subject: {Subject}", m.To, m.Subject);
        return Task.FromResult(EmailResult.Success("skipped"));
    }
}