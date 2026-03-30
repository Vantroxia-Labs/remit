namespace AegisEInvoicing.FIRSAccessPoint.Attributes;

/// <summary>
/// Marks a controller, action, or service as tenant-agnostic.
/// This indicates that the functionality operates independently of tenant boundaries
/// and should bypass multi-tenant isolation mechanisms.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = false)]
public sealed class TenantAgnosticAttribute : Attribute
{
    /// <summary>
    /// Optional reason why this component is tenant-agnostic
    /// </summary>
    public string? Reason { get; init; }

    public TenantAgnosticAttribute()
    {
    }

    public TenantAgnosticAttribute(string reason)
    {
        Reason = reason;
    }
}