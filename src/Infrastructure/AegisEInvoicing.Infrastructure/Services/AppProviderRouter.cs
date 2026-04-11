using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Interswitch.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.Services;

/// <summary>
/// Resolves and configures the correct <see cref="IAccessPointProviderClient"/> for a given business.
///
/// Looks up the business's <c>ActiveAppProviderCode</c> and <c>AppEnvironmentMode</c>,
/// fetches the matching <c>AppProviderConfiguration</c>, decrypts the right credential set,
/// calls <c>Configure()</c> on the underlying HTTP client, and returns the adapter.
///
/// Falls back to Interswitch (with appsettings credentials) when no provider code is set.
/// </summary>
public sealed class AppProviderRouter(
    IApplicationDbContext context,
    IEncryptionService encryptionService,
    IInterswitchHttpClient interswitchHttpClient,
    InterswitchAppAdapter interswitchAdapter,
    ILogger<AppProviderRouter> logger) : IAppProviderRouter
{
    private const string InterswitchProviderCode = "interswitch";

    /// <inheritdoc />
    public async Task<IAccessPointProviderClient> GetProviderAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        // Load business provider settings
        var business = await context.Businesses
            .AsNoTracking()
            .Where(b => b.Id == businessId && !b.IsDeleted)
            .Select(b => new
            {
                b.ActiveAppProviderCode,
                b.AppEnvironmentMode
            })
            .FirstOrDefaultAsync(cancellationToken);

        var providerCode = string.IsNullOrWhiteSpace(business?.ActiveAppProviderCode)
            ? InterswitchProviderCode
            : business.ActiveAppProviderCode;

        var isSandbox = business?.AppEnvironmentMode == AppEnvironmentMode.Sandbox;

        // Attempt to load provider configuration from DB
        var providerConfig = await context.AppProviderConfigurations
            .AsNoTracking()
            .Where(p => p.ProviderCode == providerCode && p.IsActive && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (providerConfig is null)
        {
            // No DB entry yet — use the adapter with appsettings-injected credentials as fallback
            logger.LogWarning(
                "No AppProviderConfiguration found for provider '{ProviderCode}' " +
                "(BusinessId={BusinessId}). Using appsettings credentials.",
                providerCode, businessId);

            return ResolveAdapter(providerCode);
        }

        // Resolve credentials for the target environment
        var (baseUrl, encryptedApiKey, encryptedApiSecret, tokenEndpoint) = isSandbox
            ? (providerConfig.SandboxBaseUrl,
               providerConfig.EncryptedSandboxApiKey,
               providerConfig.EncryptedSandboxApiSecret,
               providerConfig.SandboxTokenEndpoint)
            : (providerConfig.ProductionBaseUrl,
               providerConfig.EncryptedProductionApiKey,
               providerConfig.EncryptedProductionApiSecret,
               providerConfig.ProductionTokenEndpoint);

        var apiKey = encryptedApiKey is not null
            ? await encryptionService.DecryptAsync(encryptedApiKey)
            : string.Empty;

        var apiSecret = encryptedApiSecret is not null
            ? await encryptionService.DecryptAsync(encryptedApiSecret)
            : string.Empty;

        // Configure the underlying HTTP client with DB credentials
        if (providerCode == InterswitchProviderCode)
        {
            interswitchHttpClient.Configure(
                baseUrl,
                clientId: apiKey,
                clientSecret: apiSecret,
                tokenEndpoint: tokenEndpoint ?? "/Api/SwitchTax/Token");

            logger.LogInformation(
                "AppProviderRouter: configured Interswitch for BusinessId={BusinessId}, Environment={Env}",
                businessId, isSandbox ? "Sandbox" : "Production");
        }

        return ResolveAdapter(providerCode);
    }

    /// <summary>
    /// Returns the registered adapter for the given provider code.
    /// Extend this when BlueBridge / eTranzact adapters are added to DI.
    /// </summary>
    private IAccessPointProviderClient ResolveAdapter(string providerCode) =>
        providerCode switch
        {
            InterswitchProviderCode => interswitchAdapter,
            _ => throw new InvalidOperationException(
                $"No IAccessPointProviderClient adapter is registered for provider code '{providerCode}'. " +
                "Register the adapter in DI and update AppProviderRouter.ResolveAdapter().")
        };
}
