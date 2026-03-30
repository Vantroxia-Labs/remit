using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.GenerateLicense;

/// <summary>
/// Command to generate a license key for an on-premise business
/// Only accessible by Aegis Admin role
/// </summary>
public record GenerateLicenseCommand : IRequest<GenerateLicenseResult>
{
    /// <summary>
    /// Business ID to generate license for
    /// </summary>
    public Guid BusinessId { get; init; }

    /// <summary>
    /// License expiry date (with time)
    /// </summary>
    public DateTime ExpiryDate { get; init; }
}

/// <summary>
/// Result of license generation
/// </summary>
public class GenerateLicenseResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? LicenseKey { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int StatusCode { get; set; }

    public static GenerateLicenseResult SuccessResult(string licenseKey, DateTime issuedDate, DateTime expiryDate) =>
        new()
        {
            Success = true,
            Message = "License generated successfully",
            LicenseKey = licenseKey,
            IssuedDate = issuedDate,
            ExpiryDate = expiryDate,
            StatusCode = 200
        };

    public static GenerateLicenseResult FailureResult(string message, int statusCode = 400) =>
        new()
        {
            Success = false,
            Message = message,
            StatusCode = statusCode
        };
}
