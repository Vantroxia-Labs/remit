using System.Net;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.BlueBridge.Configuration;
using AegisEInvoicing.BlueBridge.Contracts;
using AegisEInvoicing.BlueBridge.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace AegisEInvoicing.BlueBridge;

/// <summary>
/// Extension methods for registering BlueBridge integration services.
/// </summary>
public static class BlueBridgeServiceExtensions
{
    /// <summary>
    /// Adds BlueBridge e-invoice integration services to the service collection.
    /// </summary>
    public static IServiceCollection AddBlueBridgeIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Register and validate options
        services.Configure<BlueBridgeOptions>(
            configuration.GetSection(BlueBridgeOptions.SectionName));

        services.AddSingleton<IValidateOptions<BlueBridgeOptions>, BlueBridgeOptionsValidator>();

        services.AddOptions<BlueBridgeOptions>()
            .Bind(configuration.GetSection(BlueBridgeOptions.SectionName))
            .ValidateOnStart();

        // Register main HTTP client with resilience policies
        services.AddHttpClient<IBlueBridgeClient, BlueBridgeClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<BlueBridgeOptions>>().Value;

                if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

                client.Timeout = options.RequestTimeout;

                foreach (var header in options.DefaultHeaders)
                    client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            })
            .AddPolicyHandler((sp, _) => GetRetryPolicy(sp))
            .AddPolicyHandler((sp, _) => GetCircuitBreakerPolicy(sp))
            .AddPolicyHandler(GetTimeoutPolicy());

        // Register the vendor-agnostic APP adapter
        services.AddScoped<IAccessPointProviderClient, BlueBridgeAppAdapter>();

        // Register the push-based webhook handler
        services.AddScoped<IWebhookHandler, BlueBridgeWebhookHandler>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<BlueBridgeOptions>>().Value;
        var logger = serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("BlueBridgeRetryPolicy");

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                retryCount: options.MaxRetryAttempts,
                sleepDurationProvider: attempt =>
                {
                    var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                    return baseDelay + jitter;
                },
                onRetry: (outcome, delay, attempt, _) =>
                {
                    logger.LogWarning(
                        "BlueBridge request retry attempt {Attempt}/{MaxAttempts} after {Delay}ms. " +
                        "Status: {StatusCode}, Exception: {Exception}",
                        attempt,
                        options.MaxRetryAttempts,
                        delay.TotalMilliseconds,
                        outcome.Result?.StatusCode,
                        outcome.Exception?.Message);
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<BlueBridgeOptions>>().Value;
        var logger = serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("BlueBridgeCircuitBreaker");

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: options.CircuitBreakerThreshold,
                durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds),
                onBreak: (outcome, duration) =>
                {
                    logger.LogError(
                        "BlueBridge circuit breaker opened for {Duration}s. " +
                        "Status: {StatusCode}, Exception: {Exception}",
                        duration.TotalSeconds,
                        outcome.Result?.StatusCode,
                        outcome.Exception?.Message);
                },
                onReset: () => logger.LogInformation("BlueBridge circuit breaker reset"),
                onHalfOpen: () => logger.LogWarning("BlueBridge circuit breaker half-open — testing"));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
    }
}
