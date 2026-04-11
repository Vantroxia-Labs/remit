using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Portal.API.Models.AccessPointProvider;

public class SetBusinessAppProviderRequest
{
    /// <summary>
    /// The vendor to activate for this business.
    /// Null resets to the platform default (Interswitch).
    /// </summary>
    public AppVendor? Vendor { get; set; }
}
