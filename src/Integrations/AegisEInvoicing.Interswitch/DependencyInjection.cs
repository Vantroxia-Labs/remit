using AegisEInvoicing.Interswitch.Configuration;
using AegisEInvoicing.Interswitch.Interfaces;
using AegisEInvoicing.Interswitch.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Text.Json;

namespace AegisEInvoicing.Interswitch;

/// <summary>
/// Dependency injection configuration for Interswitch integration
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Interswitch HTTP client and related services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddInterswitchIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Register options
        services.Configure<InterswitchHttpClientOptions>(
            configuration.GetSection(InterswitchHttpClientOptions.SectionName));

        // Validate options on startup
        services.AddSingleton<IValidateOptions<InterswitchHttpClientOptions>, InterswitchHttpClientOptionsValidator>();

        services.AddOptions<InterswitchHttpClientOptions>()
            .Bind(configuration.GetSection(InterswitchHttpClientOptions.SectionName))
            .ValidateOnStart();

        var httpOptions = configuration.GetSection("InterswitchHttpClient").Value;

        // Register HttpClient with Polly retry policy
        services.AddHttpClient<IInterswitchHttpClient, InterswitchHttpClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<InterswitchHttpClientOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = options.RequestTimeout;
        })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // Log retry attempt (logger will be injected in the actual implementation)
                    Console.WriteLine($"Retry attempt {retryAttempt} after {timespan.TotalSeconds}s delay");
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
    }
}

/// <summary>
/// Validates Interswitch HTTP client options
/// </summary>
public sealed class InterswitchHttpClientOptionsValidator : IValidateOptions<InterswitchHttpClientOptions>
{
    public ValidateOptionsResult Validate(string? name, InterswitchHttpClientOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            failures.Add("BaseUrl is required");
        }
        else if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) ||
                 (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            failures.Add("BaseUrl must be a valid HTTP or HTTPS URL");
        }

        if (string.IsNullOrWhiteSpace(options.ValidateIRNEndpoint))
        {
            failures.Add("ValidateIRNEndpoint is required");
        }

        if (string.IsNullOrWhiteSpace(options.ValidateInvoiceEndpoint))
        {
            failures.Add("ValidateInvoiceEndpoint is required");
        }

        if (string.IsNullOrWhiteSpace(options.SignInvoiceEndpoint))
        {
            failures.Add("SignInvoiceEndpoint is required");
        }

        if (string.IsNullOrWhiteSpace(options.TransmitInvoiceEndpoint))
        {
            failures.Add("TransmitInvoiceEndpoint is required");
        }

        if (options.RequestTimeout <= TimeSpan.Zero)
        {
            failures.Add("RequestTimeout must be greater than zero");
        }

        if (options.RequestTimeout > TimeSpan.FromMinutes(10))
        {
            failures.Add("RequestTimeout should not exceed 10 minutes");
        }

        if (options.MaxRetryAttempts < 0 || options.MaxRetryAttempts > 10)
        {
            failures.Add("MaxRetryAttempts must be between 0 and 10");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
