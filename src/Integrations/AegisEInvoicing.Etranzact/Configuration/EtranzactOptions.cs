using Microsoft.Extensions.Options;

namespace AegisEInvoicing.Etranzact.Configuration;

/// <summary>
/// Configuration options for eTranzact HTTP client.
/// Maps to "EtranzactHttpClient" section in appsettings.json.
/// Values are typically sourced from environment variables:
///   ETRANZACT_BASE_URL, ETRANZACT_CLIENT_API_KEY, ETRANZACT_CLIENT_SECRET_KEY.
/// </summary>
public sealed class EtranzactOptions
{
    public const string SectionName = "EtranzactHttpClient";

    /// <summary>
    /// eTranzact API base URL.
    /// Sourced from environment variable: base_url
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key sent in the X-API-Key header to identify the client application.
    /// Sourced from environment variable: CLIENT_API_KEY
    /// </summary>
    public string ClientApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Secret key used to compute the HMAC-SHA256 signature sent in the X-API-Signature header.
    /// Sourced from environment variable: CLIENT_SECRET_KEY
    /// </summary>
    public string ClientSecretKey { get; set; } = string.Empty;

    #region API Endpoints

    public string ValidateInvoiceEndpoint { get; set; } = "/api/v1/app/invoice/validate";
    public string SignInvoiceEndpoint { get; set; } = "/api/v1/app/invoice/sign";
    public string TransmitInvoiceEndpoint { get; set; } = "/api/v1/app/invoice/transmit";
    public string VerifyTinEndpoint { get; set; } = "/api/v1/resource/verify-tin";
    public string ValidateIrnEndpoint { get; set; } = "/api/v1/app/invoice/validate-irn";
    public string ConfirmInvoiceEndpoint { get; set; } = "/api/v1/app/invoice/confirm";
    public string UpdateInvoiceEndpoint { get; set; } = "/api/v1/app/invoice/update";

    #endregion

    /// <summary>
    /// Default headers to include in all requests.
    /// </summary>
    public Dictionary<string, string> DefaultHeaders { get; set; } = new()
    {
        { "Accept", "application/json" },
        { "User-Agent", "AndersenNigeria-Etranzact/1.0" }
    };

    /// <summary>
    /// Request timeout duration.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Maximum number of retry attempts for failed requests.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Number of failures before circuit breaker opens.
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Duration the circuit breaker stays open in seconds.
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Enable detailed request logging.
    /// </summary>
    public bool EnableRequestLogging { get; set; } = true;

    /// <summary>
    /// Enable detailed response logging.
    /// </summary>
    public bool EnableResponseLogging { get; set; } = true;
}

/// <summary>
/// Validates eTranzact configuration options.
/// </summary>
public sealed class EtranzactOptionsValidator : IValidateOptions<EtranzactOptions>
{
    public ValidateOptionsResult Validate(string? name, EtranzactOptions options)
    {
        var failures = new List<string>();

        if (!string.IsNullOrWhiteSpace(options.BaseUrl) &&
            (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            failures.Add("Etranzact BaseUrl must be a valid HTTP/HTTPS URL");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
