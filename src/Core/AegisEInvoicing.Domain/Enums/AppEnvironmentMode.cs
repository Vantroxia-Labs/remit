namespace AegisEInvoicing.Domain.Enums;

/// <summary>
/// Determines which set of APP credentials a business uses for invoice operations.
/// </summary>
public enum AppEnvironmentMode
{
    /// <summary>
    /// Sandbox/test mode — uses sandbox credentials. No real invoices are submitted to FIRS.
    /// </summary>
    Sandbox = 1,

    /// <summary>
    /// Production/live mode — uses production credentials. Real invoices are submitted to FIRS.
    /// </summary>
    Production = 2
}
