namespace AegisEInvoicing.Portal.API.Models.AccessPointProvider;

public class SetBusinessAppProviderRequest
{
    /// <summary>
    /// The adapter key to activate for this business (e.g. "interswitch", "digitax").
    /// Null or empty resets to the platform default.
    /// </summary>
    public string? AdapterKey { get; set; }
}
