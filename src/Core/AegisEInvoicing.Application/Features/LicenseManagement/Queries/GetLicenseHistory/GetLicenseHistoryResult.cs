namespace AegisEInvoicing.Application.Features.LicenseManagement.Queries.GetLicenseHistory;

/// <summary>
/// License details
/// </summary>
public class LicenseDetails
{
    public string? LicenseKey { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? DaysRemaining { get; set; }
    // Computed property
    public string Status
    {
        get
        {
            if (!ExpiryDate.HasValue)
                return "NotActivated";
            var daysRemaining = (ExpiryDate.Value - DateTime.UtcNow).TotalDays;
            if (daysRemaining < 0)
                return "Expired";
            if (daysRemaining <= 30)
                return "ExpiringSoon";
            return "Active";
        }
    } // "Active", "Expired", "ExpiringSoon", "NotActivated"
}

/// <summary>
/// Result containing current business's license information with pagination
/// </summary>
public class GetLicenseHistoryResult
{
    public List<LicenseDetails> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}