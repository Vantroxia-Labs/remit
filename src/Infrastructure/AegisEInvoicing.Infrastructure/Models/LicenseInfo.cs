namespace AegisEInvoicing.Infrastructure.Models;

public class LicenseInfo
{
    public bool IsValid { get; set; }
    public DateTimeOffset? ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
}
