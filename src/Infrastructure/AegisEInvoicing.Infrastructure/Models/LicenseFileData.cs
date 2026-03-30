namespace AegisEInvoicing.Infrastructure.Models;

public class LicenseFileData
{
    public string OrganizationName { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
    public DateTimeOffset ExpiryDate { get; set; }
    public string Signature { get; set; } = string.Empty;
}
