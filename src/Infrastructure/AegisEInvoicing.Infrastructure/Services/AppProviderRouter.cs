using System.Text.Json;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Interswitch.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.Services;

/// <summary>
/// Resolves and configures the correct <see cref="IAccessPointProviderClient"/> for a given business.
///
/// Flow:
///   1. Load business's <c>ActiveVendor</c> and <c>AppEnvironmentMode</c>.
///   2. Default to <c>AppVendor.Interswitch</c> when no vendor is set.
///   3. Fetch the matching <c>AppProviderConfiguration</c>.
///   4. Decrypt the appropriate credential blob (sandbox or production).
///   5. Deserialise the JSON and configure the vendor-specific adapter.
///   6. Return the configured adapter.
///
/// Adding a new vendor: implement <see cref="IAccessPointProviderClient"/>, register it in DI,
/// add a case to <see cref="ConfigureAndResolve"/>, and document the credential JSON schema.
/// </summary>
public sealed class AppProviderRouter(
    IApplicationDbContext context,
    IEncryptionService encryptionService,
    IInterswitchHttpClient interswitchHttpClient,
    InterswitchAppAdapter interswitchAdapter,
    ILogger<AppProviderRouter> logger) : IAppProviderRouter
{
    /// <inheritdoc />
    public async Task<IAccessPointProviderClient> GetProviderAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        // 1. Load business settings
        var business = await context.Businesses
            .AsNoTracking()
            .Where(b => b.Id == businessId && !b.IsDeleted)
            .Select(b => new { b.ActiveVendor, b.AppEnvironmentMode })
            .FirstOrDefaultAsync(cancellationToken);

        var vendor = business?.ActiveVendor ?? AppVendor.Interswitch;
        var isSandbox = business?.AppEnvironmentMode == AppEnvironmentMode.Sandbox;

        // 2. Fetch provider configuration
        var config = await context.AppProviderConfigurations
            .AsNoTracking()
            .Where(p => p.Vendor == vendor && p.IsActive && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (config is null)
        {
            // No DB entry yet — fall back to appsettings-injected credentials
            logger.LogWarning(
                "No AppProviderConfiguration found for vendor '{Vendor}' (BusinessId={BusinessId}). " +
                "Using appsettings credentials.",
                vendor, businessId);

            return ResolveAdapter(vendor);
        }

        // 3. Decrypt the right credential blob
        var encryptedBlob = isSandbox ? config.EncryptedSandboxCredentials : config.EncryptedCredentials;
        var baseUrl       = isSandbox ? (config.SandboxBaseUrl ?? config.BaseUrl) : config.BaseUrl;

        var credentialsJson = encryptedBlob is not null
            ? await encryptionService.DecryptAsync(encryptedBlob)
            : null;

        logger.LogInformation(
            "AppProviderRouter: configuring {Vendor} for BusinessId={BusinessId}, Environment={Env}",
            vendor, businessId, isSandbox ? "Sandbox" : "Production");

        // 4. Configure the adapter and return it
        return ConfigureAndResolve(vendor, baseUrl, credentialsJson);
    }

    /// <summary>
    /// Configures the vendor adapter with decrypted runtime credentials and returns it.
    /// Each vendor defines its own credential JSON schema (documented below).
    /// </summary>
    private IAccessPointProviderClient ConfigureAndResolve(
        AppVendor vendor, string baseUrl, string? credentialsJson)
    {
        switch (vendor)
        {
            case AppVendor.Interswitch:
            {
                // Credential JSON schema:
                // { "clientId": "...", "clientSecret": "...", "tokenEndpoint": "/Api/SwitchTax/Token" }
                string clientId = string.Empty, clientSecret = string.Empty;
                string tokenEndpoint = "/Api/SwitchTax/Token";

                if (credentialsJson is not null)
                {
                    using var doc = JsonDocument.Parse(credentialsJson);
                    var root = doc.RootElement;
                    clientId      = root.TryGetProperty("clientId",      out var cid) ? cid.GetString() ?? string.Empty : string.Empty;
                    clientSecret  = root.TryGetProperty("clientSecret",  out var cs)  ? cs.GetString()  ?? string.Empty : string.Empty;
                    tokenEndpoint = root.TryGetProperty("tokenEndpoint", out var te)  ? te.GetString()  ?? tokenEndpoint : tokenEndpoint;
                }

                interswitchHttpClient.Configure(baseUrl, clientId, clientSecret, tokenEndpoint);
                return interswitchAdapter;
            }

            // Future vendors: add cases here, document their credential JSON schema
            // case AppVendor.Digitax: ...
            // case AppVendor.Etranzact: ...
            // case AppVendor.BlueBridge: ...

            default:
                throw new InvalidOperationException(
                    $"No IAccessPointProviderClient adapter is registered for vendor '{vendor}'. " +
                    "Implement the adapter, register it in DI, and add a case to AppProviderRouter.");
        }
    }

    /// <summary>
    /// Returns the unconfigured adapter for a vendor (used as fallback when no DB config exists).
    /// </summary>
    private IAccessPointProviderClient ResolveAdapter(AppVendor vendor) =>
        vendor switch
        {
            AppVendor.Interswitch => interswitchAdapter,
            _ => throw new InvalidOperationException(
                $"No adapter registered for vendor '{vendor}'.")
        };
}
