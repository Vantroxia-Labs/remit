
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Application.Features.LicenseManagement.Queries.GetAllLicenses;

/// <summary>
/// Result containing all OnPremise business licenses
/// </summary>
public class GetAllLicensesResult
{
    public List<LicenseInfo> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// License information for a business
/// </summary>
public class LicenseInfo
{
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = null!;
    public string LicenseKey { get; set; } = string.Empty;
    [JsonIgnore]
    public DateTime IssuedDateValue { get; set; }
    public string IssuedDate => (IssuedDateValue == DateTime.MinValue) ? string.Empty : IssuedDateValue.ToString("dd/MMM/yyyy hh:mm:tt");
    [JsonIgnore]
    public DateTime ExpiryDateValue { get; set; }
    public string ExpiryDate => (ExpiryDateValue == DateTime.MinValue) ? string.Empty : ExpiryDateValue.ToString("dd/MMM/yyyy hh:mm:tt");
    public int DaysRemaining { get; set; }
    public string Status { get; set; } = null!; // "Active", "Expired", "ExpiringSoon", "NotActivated"
}