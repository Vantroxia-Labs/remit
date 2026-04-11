using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Create;

public class CreateAccessPointProvidersCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEncryptionService encryptionService,
    ILogger<CreateAccessPointProvidersCommandHandler> logger)
    : IRequestHandler<CreateAccessPointProvidersCommand, CreateAccessPointProvidersResult>
{
    public async Task<CreateAccessPointProvidersResult> Handle(
        CreateAccessPointProvidersCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsPlatformAdmin)
            return new CreateAccessPointProvidersResult(false, "Only AegisAdmin users may manage APP provider configurations.");

        var normalizedCode = request.ProviderCode.ToLowerInvariant().Trim();

        var exists = await context.AppProviderConfigurations
            .AnyAsync(p => p.ProviderCode == normalizedCode && !p.IsDeleted, cancellationToken);

        if (exists)
            return new CreateAccessPointProvidersResult(false, $"A provider with code '{normalizedCode}' already exists.");

        // Encrypt credentials
        var encSandboxKey    = await EncryptOptional(request.SandboxApiKey, encryptionService);
        var encSandboxSecret = await EncryptOptional(request.SandboxApiSecret, encryptionService);
        var encProdKey       = await EncryptOptional(request.ProductionApiKey, encryptionService);
        var encProdSecret    = await EncryptOptional(request.ProductionApiSecret, encryptionService);

        AppProviderConfiguration configuration = request.AuthScheme switch
        {
            AppAuthScheme.OAuth2ClientCredentials => AppProviderConfiguration.CreateOAuth2Provider(
                normalizedCode, request.DisplayName, request.Description,
                request.SandboxBaseUrl, encSandboxKey, encSandboxSecret, request.SandboxTokenEndpoint,
                request.ProductionBaseUrl, encProdKey, encProdSecret, request.ProductionTokenEndpoint),

            AppAuthScheme.StaticApiKey => AppProviderConfiguration.CreateStaticApiKeyProvider(
                normalizedCode, request.DisplayName, request.Description,
                request.ApiKeyHeaderName ?? "X-API-Key",
                request.SandboxBaseUrl, encSandboxKey,
                request.ProductionBaseUrl, encProdKey),

            AppAuthScheme.HmacApiKey => AppProviderConfiguration.CreateHmacApiKeyProvider(
                normalizedCode, request.DisplayName, request.Description,
                request.ApiKeyHeaderName ?? "X-API-Key",
                request.SignatureHeaderName ?? "X-API-Signature",
                request.SandboxBaseUrl, encSandboxKey, encSandboxSecret,
                request.ProductionBaseUrl, encProdKey, encProdSecret),

            _ => throw new ArgumentOutOfRangeException(nameof(request.AuthScheme))
        };

        await context.AppProviderConfigurations.AddAsync(configuration, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "AppProviderConfiguration created: ProviderCode={Code}, AuthScheme={Scheme}",
            normalizedCode, request.AuthScheme);

        return new CreateAccessPointProvidersResult(true, "Access point provider created successfully.", configuration.Id);
    }

    private static async Task<string?> EncryptOptional(string? value, IEncryptionService svc)
        => string.IsNullOrWhiteSpace(value) ? null : await svc.EncryptAsync(value);
}
