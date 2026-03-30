namespace AegisEInvoicing.Portal.API.Models.SystemSetup;

public record UpdateLicenseRequest
{
    public string LicenseKey { get; init; } = default!;
    public DateTimeOffset ExpiryDate { get; init; }
}
