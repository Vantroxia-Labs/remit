using System.Net;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Etranzact.Configuration;
using AegisEInvoicing.Etranzact.Contracts;
using AegisEInvoicing.Etranzact.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace AegisEInvoicing.Etranzact;

/// <summary>
/// Extension methods for registering eTranzact integration services.
/// </summary>
public static class EtranzactServiceExtensions
{
    /// <summary>
    /// Adds eTranzact e-invoice integration services to the service collection.
    /// </summary>
    public static IServiceCollection AddEtranzactIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Register and validate options
        services.Configure<EtranzactOptions>(
            configuration.GetSection(EtranzactOptions.SectionName));

        services.AddSingleton<IValidateOptions<EtranzactOptions>, EtranzactOptionsValidator>();

        services.AddOptions<EtranzactOptions>()
            .Bind(configuration.GetSection(EtranzactOptions.SectionName))
            .ValidateOnStart();

        // Register main HTTP client with resilience policies
        services.AddHttpClient<IEtranzactClient, EtranzactClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<EtranzactOptions>>().Value;

                if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                {
                    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
                }

                client.Timeout = options.RequestTimeout;

                foreach (var header in options.DefaultHeaders)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            })
            .AddPolicyHandler((sp, _) => GetRetryPolicy(sp))
            .AddPolicyHandler((sp, _) => GetCircuitBreakerPolicy(sp))
            .AddPolicyHandler(GetTimeoutPolicy());

        // Register the vendor-agnostic APP adapter
        services.AddScoped<IAccessPointProviderClient, EtranzactAppAdapter>();

        // Register the push-based webhook handler
        services.AddScoped<IWebhookHandler, EtranzactWebhookHandler>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<EtranzactOptions>>().Value;
        var logger = serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("EtranzactRetryPolicy");

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
                        "eTranzact request retry attempt {Attempt}/{MaxAttempts} after {Delay}ms. " +
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
        var options = serviceProvider.GetRequiredService<IOptions<EtranzactOptions>>().Value;
        var logger = serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("EtranzactCircuitBreaker");

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: options.CircuitBreakerThreshold,
                durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds),
                onBreak: (outcome, breakDuration) =>
                {
                    logger.LogError(
                        "eTranzact circuit breaker OPENED for {Duration}s. " +
                        "Status: {StatusCode}, Exception: {Exception}",
                        breakDuration.TotalSeconds,
                        outcome.Result?.StatusCode,
                        outcome.Exception?.Message);
                },
                onReset: () =>
                {
                    logger.LogInformation("eTranzact circuit breaker RESET - calls will be allowed through");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("eTranzact circuit breaker HALF-OPEN - testing if service recovered");
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(30),
            TimeoutStrategy.Optimistic);
    }
}
