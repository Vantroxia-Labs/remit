using AegisEInvoicing.FIRSAccessPoint.Interfaces;
using AegisEInvoicing.FIRSAccessPoint.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AegisEInvoicing.FIRSAccessPoint;

public static class DependencyInjection
{
    public static IServiceCollection AddFIRSAccessPoint(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<FIRSHttpClientOptions>(
            configuration.GetSection(FIRSHttpClientOptions.SectionName));

        services.AddScoped<IFIRSHttpClient, FIRSHttpClient>();

        services.AddSingleton<IValidateOptions<FIRSHttpClientOptions>, FIRSHttpClientOptionsValidator>();

        // Note: The IIntegrationService mapping will be done in the API startup where both interfaces are available

        return services;
    }
}

public sealed class FIRSHttpClientOptionsValidator : IValidateOptions<FIRSHttpClientOptions>
{
    public ValidateOptionsResult Validate(string? name, FIRSHttpClientOptions options)
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

        if (string.IsNullOrWhiteSpace(options.AuthenticationEndpoint))
        {
            failures.Add("AuthenticationEndpoint is required");
        }

        if (string.IsNullOrWhiteSpace(options.ValidateInvoiceDataEndpoint))
        {
            failures.Add("ValidateInvoiceDataEndpoint is required");
        }

        if (string.IsNullOrWhiteSpace(options.SignInvoiceEndpoint))
        {
            failures.Add("SignInvoiceEndpoint is required");
        }

        if (string.IsNullOrWhiteSpace(options.ReportInvoiceEndpoint))
        {
            failures.Add("ReportInvoiceEndpoint is required");
        }

        if (options.RequestTimeout <= TimeSpan.Zero)
        {
            failures.Add("RequestTimeout must be greater than zero");
        }

        if (options.RequestTimeout > TimeSpan.FromMinutes(10))
        {
            failures.Add("RequestTimeout should not exceed 10 minutes");
        }

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}