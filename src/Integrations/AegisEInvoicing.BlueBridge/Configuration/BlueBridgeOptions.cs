using Microsoft.Extensions.Options;

namespace AegisEInvoicing.BlueBridge.Configuration;

/// <summary>
/// Configuration options for the BlueBridge HTTP client.
/// Maps to "BlueBridgeHttpClient" section in appsettings.json.
/// </summary>
public sealed class BlueBridgeOptions
{
    public const string SectionName = "BlueBridgeHttpClient";

    /// <summary>
    /// BlueBridge API base URL.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key sent in the X-API-Key header to authenticate requests.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    #region API Endpoints

    public string GenerateIrnEndpoint { get; set; } = "api/v1/invoices/generate-irn";
    public string ValidateIrnEndpoint { get; set; } = "api/v1/invoices/validate-irn";
    public string ValidateInvoiceEndpoint { get; set; } = "api/v1/invoices/validate";
    public string SignInvoiceEndpoint { get; set; } = "api/v1/invoices/sign";
    public string GenerateQrCodeEndpoint { get; set; } = "api/v1/invoices/generate-qrcode";
    public string TransmitInvoiceEndpoint { get; set; } = "api/v1/invoices/transmit";
    public string LookupWithTinEndpoint { get; set; } = "api/v1/invoices/transmit/lookup/tin";
    public string LookupWithIrnEndpoint { get; set; } = "api/v1/invoices/lookup";
    public string UpdateInvoiceEndpoint { get; set; } = "api/v1/invoices/update";
    public string SearchInvoicesEndpoint { get; set; } = "api/v1/invoices";
    public string HealthEndpoint { get; set; } = "api/v1/invoices/health";
    public string ConfirmInvoiceEndpoint { get; set; } = "api/v1/invoices/confirm";

    #endregion

    /// <summary>
    /// Default headers included in all requests.
    /// </summary>
    public Dictionary<string, string> DefaultHeaders { get; set; } = new()
    {
        { "Accept", "application/json" },
        { "User-Agent", "AndersenNigeria-BlueBridge/1.0" }
    };

    /// <summary>
    /// Request timeout duration.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Maximum number of retry attempts for transient failures.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Number of consecutive failures before the circuit breaker opens.
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Duration in seconds the circuit breaker remains open before half-opening.
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
/// Validates BlueBridge configuration options at startup.
/// </summary>
public sealed class BlueBridgeOptionsValidator : IValidateOptions<BlueBridgeOptions>
{
    public ValidateOptionsResult Validate(string? name, BlueBridgeOptions options)
    {
        var failures = new List<string>();

        if (!string.IsNullOrWhiteSpace(options.BaseUrl) &&
            (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            failures.Add("BlueBridge BaseUrl must be a valid HTTP/HTTPS URL");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
