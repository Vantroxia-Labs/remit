namespace AegisEInvoicing.Infrastructure.Services.Licensing.Models;

/// <summary>
/// Request model for generating a license key
/// </summary>
public class GenerateLicenseRequest
{
    /// <summary>
    /// Application name (e.g., "EInvoicing")
    /// </summary>
    public string AppName { get; set; } = null!;

    /// <summary>
    /// Application version (e.g., "V1")
    /// </summary>
    public string AppVersion { get; set; } = null!;

    /// <summary>
    /// Client ID (Business ID)
    /// </summary>
    public string ClientId { get; set; } = null!;

    /// <summary>
    /// License expiry date (with time)
    /// </summary>
    public DateTime ExpiryDate { get; set; }
}

/// <summary>
/// Response model from generate license endpoint
/// </summary>
public class GenerateLicenseResponse
{
    public int Status { get; set; }
    public string? Data { get; set; } // License key
    public string? Message { get; set; }
}

/// <summary>
/// Response model from validate-license-key endpoint (detailed validation)
/// </summary>
public class ValidateLicenseKeyResponse
{
    public int Status { get; set; }
    public ValidateLicenseKeyData? Data { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Detailed license key validation data
/// </summary>
public class ValidateLicenseKeyData
{
    public string? AppName { get; set; }
    public string? AppVersion { get; set; }
    public string? ClientId { get; set; }
    public string? ExpiryDate { get; set; } // Format: "yyyy-MM-dd"
    public string? Status { get; set; } // "Active", "Expired", etc.
}



