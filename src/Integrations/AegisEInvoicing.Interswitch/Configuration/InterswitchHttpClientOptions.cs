namespace AegisEInvoicing.Interswitch.Configuration;

/// <summary>
/// Configuration options for Interswitch HTTP client.
/// Maps to "InterswitchHttpClient" section in appsettings.json
/// </summary>
public sealed class InterswitchHttpClientOptions
{
    public const string SectionName = "InterswitchHttpClient";

    /// <summary>
    /// 
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Client ID for Interswitch authentication
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client Secret for Interswitch authentication
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Token API endpoint for authentication
    /// </summary>
    public string TokenEndpoint { get; set; } = "/Api/SwitchTax/Token";

    /// <summary>
    /// API endpoints configuration
    /// </summary>
    public string ValidateIRNEndpoint { get; set; } = "/Api/SwitchTax/ValidateIRN";
    public string ValidateInvoiceEndpoint { get; set; } = "/Api/SwitchTax/ValidateInvoice";
    public string ConfirmInvoiceEndpoint { get; set; } = "/Api/SwitchTax/ConfirmInvoice";
    public string SignInvoiceEndpoint { get; set; } = "/Api/SwitchTax/SignInvoice";
    public string UpdateStatusEndpoint { get; set; } = "/Api/SwitchTax/UpdateStatus";
    public string DownloadInvoiceEndpoint { get; set; } = "/Api/SwitchTax/DownloadInvoice";
    public string SearchInvoiceEndpoint { get; set; } = "/Api/SwitchTax/SearchInvoice";
    public string LookupWithIRNEndpoint { get; set; } = "/Api/SwitchTax/LookupWithIRN";
    public string TransmitInvoiceEndpoint { get; set; } = "/Api/SwitchTax/Transmit";
    public string LookupWithTINEndpoint { get; set; } = "/Api/SwitchTax/LookupWithTIN";
    public string GetEntityEndpoint { get; set; } = "/Api/SwitchTax/GetEntity";
    public string GetPurchaseInvoicesEndpoint { get; set; } = "/Api/SwitchTax/GetPurchases";

    /// <summary>
    /// API version (e.g., "v1")
    /// </summary>
    public string ApiVersion { get; set; } = "v1";

    /// <summary>
    /// Default headers to include in all requests
    /// Note: Content-Type is set per-request on HttpContent, not here
    /// </summary>
    public Dictionary<string, string> DefaultHeaders { get; set; } = new()
    {
        { "Accept", "application/json" },
        { "User-Agent", "AegisEInvoicing-Interswitch/1.0" }
    };

    public List<string> SensitiveDataPatterns { get; set; } =
    [
        @"(?i)(password|pwd|secret|key|token|auth)[""\s]*[:=][""\s]*[^""\s,}]+",
        @"(?i)(ssn|social[-\s]?security|tax[-\s]?id)[""\s]*[:=][""\s]*[\d-]+",
        @"(?i)(credit[-\s]?card|cc|card[-\s]?number)[""\s]*[:=][""\s]*[\d\s-]+",
        @"(?i)(email|e[-\s]?mail)[""\s]*[:=][""\s]*[^""\s,}]+@[^""\s,}]+",
        @"(?i)(phone|mobile|tel)[""\s]*[:=][""\s]*[\+\d\s()-]+"
    ];

    /// <summary>
    /// Request timeout duration
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Maximum number of retry attempts for failed requests
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Enable detailed request logging
    /// </summary>
    public bool EnableRequestLogging { get; set; } = true;

    /// <summary>
    /// Enable detailed response logging
    /// </summary>
    public bool EnableResponseLogging { get; set; } = true;

    /// <summary>
    /// Indicates that this service operates independently of tenant boundaries.
    /// Interswitch integration is a shared service across all tenants.
    /// </summary>
    public bool IsTenantAgnostic { get; set; } = true;
}
