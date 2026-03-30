using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.LicenseManagement.Commands.ActivateLicense;

/// <summary>
/// Result of license activation
/// </summary>
public class ActivateLicenseResult
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public string? LicenseKey { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? DaysRemaining { get; set; }
    public string? Status { get; set; }

    public static ActivateLicenseResult SuccessResult(
        string licenseKey,
        DateTime issuedDate,
        DateTime expiryDate,
        string status)
    {
        var daysRemaining = (int)(expiryDate - DateTime.UtcNow).TotalDays;

        return new ActivateLicenseResult
        {
            Success = true,
            StatusCode = (int)HttpStatusCodes.Created,
            Message = "License activated successfully",
            LicenseKey = licenseKey,
            IssuedDate = issuedDate,
            ExpiryDate = expiryDate,
            DaysRemaining = daysRemaining,
            Status = status
        };
    }

    public static ActivateLicenseResult FailureResult(string message, int statusCode = 400)
    {
        return new ActivateLicenseResult
        {
            Success = false,
            StatusCode = statusCode,
            Message = message
        };
    }
}
