namespace AegisEInvoicing.Domain.Enums;

/// <summary>
/// Tax authority that receives the WHT remittance.
/// B2B/B2G transactions → NRS (Federal); B2C (individual payee) → StateIRS.
/// </summary>
public enum WhtTaxAuthority
{
    /// <summary>Nigeria Revenue Service (Federal) — for company/government payees.</summary>
    NRS = 0,
    /// <summary>State Internal Revenue Service — for individual payees (PITA-governed income).</summary>
    StateIRS = 1,
}
