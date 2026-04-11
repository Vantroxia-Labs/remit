using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.Services;

/// <summary>
/// Resolves and configures the correct <see cref="IAccessPointProviderClient"/> for a given business.
///
/// Flow:
///   1. Load business's <c>ActiveAdapterKey</c> and <c>AppEnvironmentMode</c>.
///   2. Default to "interswitch" when no adapter key is set.
///   3. Look up the adapter in the DI-injected registry (keyed by ProviderCode).
///   4. Fetch the matching <c>AppProviderConfiguration</c> from the DB.
///   5. Decrypt the appropriate credential blob (sandbox or production).
///   6. Call adapter.Configure(baseUrl, credentialsJson) and return the adapter.
///
/// Adding a new provider: implement <see cref="IAccessPointProviderClient"/>,
/// register it in DI as IAccessPointProviderClient, and create an AppProviderConfiguration
/// row with the matching AdapterKey. Zero code changes required here.
/// </summary>
public sealed class AppProviderRouter(
    IApplicationDbContext context,
    IEncryptionService encryptionService,
    IEnumerable<IAccessPointProviderClient> adapters,
    ILogger<AppProviderRouter> logger) : IAppProviderRouter
{
    private const string DefaultAdapterKey = "interswitch";

    // Keyed by ProviderCode (lowercase) — built once per DI scope from registered adapters.
    private readonly Dictionary<string, IAccessPointProviderClient> _registry =
        adapters.ToDictionary(a => a.ProviderCode, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<IAccessPointProviderClient> GetProviderAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        // 1. Load business settings
        var business = await context.Businesses
            .AsNoTracking()
            .Where(b => b.Id == businessId && !b.IsDeleted)
            .Select(b => new { b.ActiveAdapterKey, b.AppEnvironmentMode })
            .FirstOrDefaultAsync(cancellationToken);

        var adapterKey = business?.ActiveAdapterKey ?? DefaultAdapterKey;
        var isSandbox  = business?.AppEnvironmentMode == AppEnvironmentMode.Sandbox;

        // 2. Resolve adapter from registry
        if (!_registry.TryGetValue(adapterKey, out var adapter))
        {
            logger.LogError(
                "No IAccessPointProviderClient registered for AdapterKey '{Key}' (BusinessId={BusinessId}). " +
                "Falling back to default ('{Default}').",
                adapterKey, businessId, DefaultAdapterKey);

            if (!_registry.TryGetValue(DefaultAdapterKey, out adapter))
                throw new InvalidOperationException(
                    $"No adapter registered for key '{adapterKey}' and default '{DefaultAdapterKey}' is also missing.");
        }

        // 3. Fetch provider configuration from DB
        var config = await context.AppProviderConfigurations
            .AsNoTracking()
            .Where(p => p.AdapterKey == adapterKey && p.IsActive && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (config is null)
        {
            // No DB entry yet — return the adapter with appsettings-injected (default) credentials
            logger.LogWarning(
                "No AppProviderConfiguration found for AdapterKey '{Key}' (BusinessId={BusinessId}). " +
                "Using appsettings credentials.",
                adapterKey, businessId);

            return adapter;
        }

        // 4. Decrypt the right credential blob
        var encryptedBlob = isSandbox ? config.EncryptedSandboxCredentials : config.EncryptedCredentials;
        var baseUrl       = isSandbox ? (config.SandboxBaseUrl ?? config.BaseUrl) : config.BaseUrl;

        var credentialsJson = encryptedBlob is not null
            ? await encryptionService.DecryptAsync(encryptedBlob)
            : null;

        logger.LogInformation(
            "AppProviderRouter: configuring '{Key}' for BusinessId={BusinessId}, Environment={Env}",
            adapterKey, businessId, isSandbox ? "Sandbox" : "Production");

        // 5. Configure the adapter with runtime credentials and return it
        adapter.Configure(baseUrl, credentialsJson);
        return adapter;
    }
}
