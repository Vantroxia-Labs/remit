namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Result returned by common APP provider operations.
/// </summary>
public sealed record AppProviderResult(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage,
    string? RawResponse)
{
    public static AppProviderResult Success(string? rawResponse = null)
        => new(true, null, null, rawResponse);

    public static AppProviderResult Failure(string errorCode, string errorMessage, string? rawResponse = null)
        => new(false, errorCode, errorMessage, rawResponse);
}

/// <summary>
/// Result of a TIN lookup operation.
/// </summary>
public sealed record AppLookupTinResult(
    bool IsSuccess,
    bool IsUp,
    string? BusinessReference,
    string? ErrorMessage);

/// <summary>
/// Vendor-agnostic interface for Access Point Provider (APP) operations.
/// Implemented by each provider adapter (Interswitch, BlueBridge, eTranzact, …).
/// </summary>
public interface IAccessPointProviderClient
{
    /// <summary>\n    /// Lowercase provider code matching the AppProviderConfiguration.ProviderCode property.\n    /// </summary>
    string ProviderCode { get; }

    /// <summary>
    /// Whether this provider supports TIN lookup before transmission.
    /// True for Interswitch; false for BlueBridge and eTranzact.
    /// </summary>
    bool SupportsLookupTin { get; }

    /// <summary>
    /// Transmit a signed invoice (by IRN) to the NRS portal via this provider.
    /// </summary>
    Task<AppProviderResult> TransmitAsync(string irn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup a business by TIN to verify enrollment on the NRS portal.
    /// Returns a failure result if <see cref="SupportsLookupTin"/> is false.
    /// </summary>
    Task<AppLookupTinResult> LookupTinAsync(string tin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Health-check ping to verify the provider's API is reachable.
    /// </summary>
    Task<(bool IsHealthy, string? Message)> PingAsync(CancellationToken cancellationToken = default);
}
