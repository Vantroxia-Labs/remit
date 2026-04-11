using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Domain.Entities;

/// <summary>
/// Stores Access Point Provider (APP) credentials and authentication configuration.
/// Each record represents one provider (e.g., Interswitch, BlueBridge, eTranzact)
/// with separate sandbox and production credential sets.
///
/// ProviderCode is a free string — new providers can be added by the AegisAdmin
/// through the admin UI without code changes (as long as a code adapter is registered
/// for that provider code in Stage 3).
///
/// Auth header names and token endpoints are stored here so that adding a new provider
/// with a different header convention requires only a DB entry, not a deployment.
/// </summary>
public class AppProviderConfiguration : AuditableEntity
{
    // ─── Provider Identity ────────────────────────────────────────────────────

    /// <summary>
    /// Unique machine-readable identifier for the provider.
    /// e.g., "interswitch", "bluebridge", "etranzact".
    /// Used by the router to resolve the correct adapter.
    /// </summary>
    public string ProviderCode { get; private set; } = default!;

    /// <summary>
    /// Human-readable name shown in the admin UI.
    /// e.g., "Interswitch SwitchTax", "BlueBridge", "eTranzact".
    /// </summary>
    public string DisplayName { get; private set; } = default!;

    /// <summary>
    /// Optional description of the provider.
    /// </summary>
    public string Description { get; private set; } = default!;

    // ─── Auth Configuration ───────────────────────────────────────────────────

    /// <summary>
    /// The authentication protocol this provider uses.
    /// Determines how ApiKey/ApiSecret/TokenEndpoint fields are interpreted.
    /// </summary>
    public AppAuthScheme AuthScheme { get; private set; }

    /// <summary>
    /// Header name used to send the API key (for StaticApiKey and HmacApiKey schemes).
    /// e.g., "X-API-Key".
    /// Null for OAuth2ClientCredentials (uses Authorization: Bearer instead).
    /// </summary>
    public string? ApiKeyHeaderName { get; private set; }

    /// <summary>
    /// Header name used to send the HMAC signature (HmacApiKey scheme only).
    /// e.g., "X-API-Signature".
    /// Null for all other schemes.
    /// </summary>
    public string? SignatureHeaderName { get; private set; }

    // ─── Sandbox Credentials ─────────────────────────────────────────────────

    public string SandboxBaseUrl { get; private set; } = default!;

    /// <summary>
    /// AES-256-GCM encrypted sandbox API key (ClientId for OAuth2, ApiKey for others).
    /// </summary>
    public string? EncryptedSandboxApiKey { get; private set; }

    /// <summary>
    /// AES-256-GCM encrypted sandbox API secret (ClientSecret for OAuth2, HMAC secret for HmacApiKey).
    /// Null for StaticApiKey scheme.
    /// </summary>
    public string? EncryptedSandboxApiSecret { get; private set; }

    /// <summary>
    /// Token endpoint path for OAuth2 sandbox (e.g., "/Api/SwitchTax/Token").
    /// Null for non-OAuth2 schemes.
    /// </summary>
    public string? SandboxTokenEndpoint { get; private set; }

    // ─── Production Credentials ───────────────────────────────────────────────

    public string ProductionBaseUrl { get; private set; } = default!;

    /// <summary>
    /// AES-256-GCM encrypted production API key.
    /// </summary>
    public string? EncryptedProductionApiKey { get; private set; }

    /// <summary>
    /// AES-256-GCM encrypted production API secret.
    /// </summary>
    public string? EncryptedProductionApiSecret { get; private set; }

    /// <summary>
    /// Token endpoint path for OAuth2 production.
    /// </summary>
    public string? ProductionTokenEndpoint { get; private set; }

    // ─── Status ───────────────────────────────────────────────────────────────

    public bool IsActive { get; private set; }

    // ─── EF Constructor ───────────────────────────────────────────────────────

    private AppProviderConfiguration() { }

    // ─── Factory Methods ──────────────────────────────────────────────────────

    public static AppProviderConfiguration CreateOAuth2Provider(
        string providerCode,
        string displayName,
        string description,
        string sandboxBaseUrl,
        string? encryptedSandboxApiKey,
        string? encryptedSandboxApiSecret,
        string? sandboxTokenEndpoint,
        string productionBaseUrl,
        string? encryptedProductionApiKey,
        string? encryptedProductionApiSecret,
        string? productionTokenEndpoint)
    {
        return new AppProviderConfiguration
        {
            ProviderCode = providerCode.ToLowerInvariant().Trim(),
            DisplayName = displayName,
            Description = description,
            AuthScheme = AppAuthScheme.OAuth2ClientCredentials,
            ApiKeyHeaderName = null,
            SignatureHeaderName = null,
            SandboxBaseUrl = sandboxBaseUrl,
            EncryptedSandboxApiKey = encryptedSandboxApiKey,
            EncryptedSandboxApiSecret = encryptedSandboxApiSecret,
            SandboxTokenEndpoint = sandboxTokenEndpoint,
            ProductionBaseUrl = productionBaseUrl,
            EncryptedProductionApiKey = encryptedProductionApiKey,
            EncryptedProductionApiSecret = encryptedProductionApiSecret,
            ProductionTokenEndpoint = productionTokenEndpoint,
            IsActive = true
        };
    }

    public static AppProviderConfiguration CreateStaticApiKeyProvider(
        string providerCode,
        string displayName,
        string description,
        string apiKeyHeaderName,
        string sandboxBaseUrl,
        string? encryptedSandboxApiKey,
        string productionBaseUrl,
        string? encryptedProductionApiKey)
    {
        return new AppProviderConfiguration
        {
            ProviderCode = providerCode.ToLowerInvariant().Trim(),
            DisplayName = displayName,
            Description = description,
            AuthScheme = AppAuthScheme.StaticApiKey,
            ApiKeyHeaderName = apiKeyHeaderName,
            SignatureHeaderName = null,
            SandboxBaseUrl = sandboxBaseUrl,
            EncryptedSandboxApiKey = encryptedSandboxApiKey,
            EncryptedSandboxApiSecret = null,
            SandboxTokenEndpoint = null,
            ProductionBaseUrl = productionBaseUrl,
            EncryptedProductionApiKey = encryptedProductionApiKey,
            EncryptedProductionApiSecret = null,
            ProductionTokenEndpoint = null,
            IsActive = true
        };
    }

    public static AppProviderConfiguration CreateHmacApiKeyProvider(
        string providerCode,
        string displayName,
        string description,
        string apiKeyHeaderName,
        string signatureHeaderName,
        string sandboxBaseUrl,
        string? encryptedSandboxApiKey,
        string? encryptedSandboxApiSecret,
        string productionBaseUrl,
        string? encryptedProductionApiKey,
        string? encryptedProductionApiSecret)
    {
        return new AppProviderConfiguration
        {
            ProviderCode = providerCode.ToLowerInvariant().Trim(),
            DisplayName = displayName,
            Description = description,
            AuthScheme = AppAuthScheme.HmacApiKey,
            ApiKeyHeaderName = apiKeyHeaderName,
            SignatureHeaderName = signatureHeaderName,
            SandboxBaseUrl = sandboxBaseUrl,
            EncryptedSandboxApiKey = encryptedSandboxApiKey,
            EncryptedSandboxApiSecret = encryptedSandboxApiSecret,
            SandboxTokenEndpoint = null,
            ProductionBaseUrl = productionBaseUrl,
            EncryptedProductionApiKey = encryptedProductionApiKey,
            EncryptedProductionApiSecret = encryptedProductionApiSecret,
            ProductionTokenEndpoint = null,
            IsActive = true
        };
    }

    // ─── Behaviour ────────────────────────────────────────────────────────────

    public void UpdateCredentials(
        string displayName,
        string description,
        string sandboxBaseUrl,
        string? encryptedSandboxApiKey,
        string? encryptedSandboxApiSecret,
        string? sandboxTokenEndpoint,
        string productionBaseUrl,
        string? encryptedProductionApiKey,
        string? encryptedProductionApiSecret,
        string? productionTokenEndpoint,
        string? apiKeyHeaderName,
        string? signatureHeaderName)
    {
        DisplayName = displayName;
        Description = description;
        SandboxBaseUrl = sandboxBaseUrl;
        EncryptedSandboxApiKey = encryptedSandboxApiKey;
        EncryptedSandboxApiSecret = encryptedSandboxApiSecret;
        SandboxTokenEndpoint = sandboxTokenEndpoint;
        ProductionBaseUrl = productionBaseUrl;
        EncryptedProductionApiKey = encryptedProductionApiKey;
        EncryptedProductionApiSecret = encryptedProductionApiSecret;
        ProductionTokenEndpoint = productionTokenEndpoint;
        ApiKeyHeaderName = apiKeyHeaderName;
        SignatureHeaderName = signatureHeaderName;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
