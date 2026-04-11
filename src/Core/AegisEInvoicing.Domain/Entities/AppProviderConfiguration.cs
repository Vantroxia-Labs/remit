using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Domain.Entities;

/// <summary>
/// Stores the configuration and encrypted credentials for an Access Point Provider (APP).
/// One record per vendor — only Andersen/AegisAdmin creates and manages these.
///
/// Credentials are stored as an AES-256-GCM encrypted JSON blob.
/// The shape of the JSON differs per vendor and is known only to the infrastructure layer
/// (AppProviderRouter), which decrypts and deserialises it when configuring the adapter.
///
/// Businesses select a vendor; the router resolves the matching configuration.
/// </summary>
public class AppProviderConfiguration : AuditableEntity
{
    /// <summary>Human-readable name. e.g., "Interswitch SwitchTax".</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Optional description shown in the admin UI.</summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Identifies which integration library handles this provider.
    /// Also used as the routing key when a business selects a vendor.
    /// </summary>
    public AppVendor Vendor { get; private set; }

    /// <summary>Production base URL. e.g., "https://api.interswitchgroup.com".</summary>
    public string BaseUrl { get; private set; } = default!;

    /// <summary>
    /// AES-256-GCM encrypted JSON blob of production credentials.
    /// The JSON schema is vendor-specific and parsed by the router.
    /// e.g., for Interswitch: { "clientId": "...", "clientSecret": "...", "tokenEndpoint": "..." }
    /// </summary>
    public string? EncryptedCredentials { get; private set; }

    /// <summary>Sandbox/test base URL. Null if this vendor has no sandbox environment.</summary>
    public string? SandboxBaseUrl { get; private set; }

    /// <summary>AES-256-GCM encrypted JSON blob of sandbox credentials. Same schema as production.</summary>
    public string? EncryptedSandboxCredentials { get; private set; }

    /// <summary>Whether this provider is available for businesses to select.</summary>
    public bool IsActive { get; private set; }

    // For EF Core
    private AppProviderConfiguration() { }

    // ─── Factory ─────────────────────────────────────────────────────────────

    public static AppProviderConfiguration Create(
        string name,
        string? description,
        AppVendor vendor,
        string baseUrl,
        string? encryptedCredentials,
        string? sandboxBaseUrl,
        string? encryptedSandboxCredentials)
    {
        return new AppProviderConfiguration
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            Vendor = vendor,
            BaseUrl = baseUrl.Trim(),
            EncryptedCredentials = encryptedCredentials,
            SandboxBaseUrl = sandboxBaseUrl?.Trim(),
            EncryptedSandboxCredentials = encryptedSandboxCredentials,
            IsActive = true
        };
    }

    // ─── Behaviour ────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates display details and, optionally, credentials.
    /// Pass null for a credential argument to keep the existing encrypted value.
    /// </summary>
    public void Update(
        string name,
        string? description,
        string? baseUrl,
        string? encryptedCredentials,
        string? sandboxBaseUrl,
        string? encryptedSandboxCredentials)
    {
        Name = name.Trim();
        Description = description?.Trim();

        if (!string.IsNullOrWhiteSpace(baseUrl))
            BaseUrl = baseUrl.Trim();

        if (encryptedCredentials is not null)
            EncryptedCredentials = encryptedCredentials;

        // SandboxBaseUrl can be explicitly cleared by passing an empty string
        SandboxBaseUrl = string.IsNullOrWhiteSpace(sandboxBaseUrl) ? null : sandboxBaseUrl.Trim();

        if (encryptedSandboxCredentials is not null)
            EncryptedSandboxCredentials = encryptedSandboxCredentials;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
