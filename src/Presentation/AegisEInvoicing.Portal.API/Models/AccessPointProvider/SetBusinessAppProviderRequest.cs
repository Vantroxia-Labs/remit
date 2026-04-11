namespace AegisEInvoicing.Portal.API.Models.AccessPointProvider;

public class SetBusinessAppProviderRequest
{
    /// <summary>
    /// Lowercase provider code (e.g. "interswitch", "bluebridge", "etranzact").
    /// Pass null to reset to the platform default (Interswitch).
    /// </summary>
    public string? ProviderCode { get; set; }
}
