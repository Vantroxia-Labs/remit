namespace AegisEInvoicing.Infrastructure.Services.Licensing;

/// <summary>
/// Configuration options for the Licensing Service (On-Premise deployments)
/// </summary>
public class LicensingServiceOptions
{
    public const string SectionName = "LicensingService";

    /// <summary>
    /// Base URL of the licensing service
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:44374";

    /// <summary>
    /// Authorization key for licensing service API calls
    /// </summary>
    public string AuthorizationKey { get; set; } = "K9MGD1g1talFact0ryAdm1n";

    /// <summary>
    /// Endpoint for generating license keys
    /// </summary>
    public string GenerateLicenseEndpoint { get; set; } = "/api/v1/license/generate-license-key";

    /// <summary>
    /// Endpoint for validating license keys
    /// </summary>
    public string ValidateLicenseEndpoint { get; set; } = "/api/v1/license/validate-license-key";

    /// <summary>
    /// Request timeout for licensing service calls
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of retry attempts for transient failures
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 2;

    /// <summary>
    /// Enable or disable request logging
    /// </summary>
    public bool EnableRequestLogging { get; set; } = true;

    /// <summary>
    /// Enable or disable response logging
    /// </summary>
    public bool EnableResponseLogging { get; set; } = true;
}
