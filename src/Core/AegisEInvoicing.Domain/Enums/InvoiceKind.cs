namespace AegisEInvoicing.Domain.Enums;

/// <summary>
/// Represents the kind/nature of invoice transaction
/// </summary>
public enum InvoiceKind
{
    /// <summary>
    /// Business to Consumer
    /// </summary>
    B2C,
    
    /// <summary>
    /// Business to Business
    /// </summary>
    B2B,
    
    /// <summary>
    /// Business to Government
    /// </summary>
    B2G
}
