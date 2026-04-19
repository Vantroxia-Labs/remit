namespace AegisEInvoicing.Domain.Enums;

/// <summary>
/// Nature of transaction for WHT classification per FIRS/NRS schedule requirements.
/// </summary>
public enum WhtNatureOfTransaction
{
    Dividends = 0,
    Interest = 1,
    Royalty = 2,
    Rent = 3,
    /// <summary>Commission, consultancy, technical, management or professional fees</summary>
    Consultancy = 4,
    /// <summary>Supply of goods or materials (not manufactured/produced by the supplier)</summary>
    SupplyOfGoods = 5,
    /// <summary>Construction — roads, bridges, buildings, power plants</summary>
    Construction = 6,
    /// <summary>Other construction and related activities</summary>
    OtherConstruction = 7,
    /// <summary>Services not specifically listed</summary>
    Services = 8,
    Brokerage = 9,
    CoLocationTelecom = 10,
    DirectorsFees = 11,
    /// <summary>Winnings, entertainment, sports, other transactions not listed above</summary>
    Other = 12,
}
