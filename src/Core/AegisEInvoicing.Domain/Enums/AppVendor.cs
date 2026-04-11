namespace AegisEInvoicing.Domain.Enums;

/// <summary>
/// Identifies the Access Point Provider vendor.
/// Each vendor has its own integration library and credential schema.
/// The router uses this to select the right adapter and deserialize credentials.
/// </summary>
public enum AppVendor
{
    Interswitch = 1,
    Digitax = 2,
    Etranzact = 3,
    BlueBridge = 4
}
