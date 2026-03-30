namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for license validation and generation (On-Premise deployments)
/// </summary>
public interface ILicensingService
{
    /// <summary>
    /// Generates a license key for an on-premise business
    /// </summary>
    Task<LicenseGenerationResult> GenerateLicenseAsync(
        string businessId,
        DateTime expiryDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a license key with detailed information
    /// Supports both fail-open (login flow) and fail-closed (activation) strategies
    /// </summary>
    /// <param name="licenseKey">License key to validate</param>
    /// <param name="failOpen">If true, allows operation on service errors (default: false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed validation result</returns>
    Task<LicenseKeyValidationResult> ValidateLicenseKeyAsync(
        string licenseKey,
        bool failOpen = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of license generation
/// </summary>
public class LicenseGenerationResult
{
    public int Status { get; set; }
    public string? LicenseKey { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Result of detailed license key validation (for activation and login)
/// </summary>
public class LicenseKeyValidationResult
{
    public bool IsValid { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public string? ClientId { get; set; }
    public string? AppName { get; set; }
    public string? AppVersion { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Status { get; set; } // "Active", "Expired", etc.
    public bool IsFailOpen { get; set; } // True if validated via fail-open strategy

    public static LicenseKeyValidationResult Success(
        string clientId,
        string appName,
        string appVersion,
        DateTime expiryDate,
        string status) =>
        new()
        {
            IsValid = true,
            StatusCode = 200,
            Message = "Key validated successfully",
            ClientId = clientId,
            AppName = appName,
            AppVersion = appVersion,
            ExpiryDate = expiryDate,
            Status = status,
            IsFailOpen = false
        };

    public static LicenseKeyValidationResult FailOpen(string message) =>
        new()
        {
            IsValid = true,  // Allow operation to proceed
            StatusCode = 503,
            Message = $"License service unavailable (fail-open): {message}",
            IsFailOpen = true
        };

    public static LicenseKeyValidationResult Failure(string message, int statusCode = 400) =>
        new()
        {
            IsValid = false,
            StatusCode = statusCode,
            Message = message,
            IsFailOpen = false
        };
}


