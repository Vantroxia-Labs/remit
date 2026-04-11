using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Update;

public class UpdateAccessPointProvidersCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEncryptionService encryptionService,
    ILogger<UpdateAccessPointProvidersCommandHandler> logger)
    : IRequestHandler<UpdateAccessPointProvidersCommand, UpdateAccessPointProvidersResult>
{
    public async Task<UpdateAccessPointProvidersResult> Handle(
        UpdateAccessPointProvidersCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsPlatformAdmin)
            return new UpdateAccessPointProvidersResult(false, "Only AegisAdmin users may manage APP provider configurations.");

        var config = await context.AppProviderConfigurations
            .FirstOrDefaultAsync(p => p.Id == request.ConfigurationId && !p.IsDeleted, cancellationToken);

        if (config is null)
            return new UpdateAccessPointProvidersResult(false, "Access point provider configuration not found.");

        var encSandboxKey    = await EncryptOptional(request.SandboxApiKey, encryptionService);
        var encSandboxSecret = await EncryptOptional(request.SandboxApiSecret, encryptionService);
        var encProdKey       = await EncryptOptional(request.ProductionApiKey, encryptionService);
        var encProdSecret    = await EncryptOptional(request.ProductionApiSecret, encryptionService);

        config.UpdateCredentials(
            request.DisplayName,
            request.Description,
            request.SandboxBaseUrl,
            encSandboxKey,
            encSandboxSecret,
            request.SandboxTokenEndpoint,
            request.ProductionBaseUrl,
            encProdKey,
            encProdSecret,
            request.ProductionTokenEndpoint,
            request.ApiKeyHeaderName,
            request.SignatureHeaderName);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "AppProviderConfiguration updated: Id={Id}, ProviderCode={Code}",
            config.Id, config.ProviderCode);

        return new UpdateAccessPointProvidersResult(true, "Access point provider updated successfully.");
    }

    private static async Task<string?> EncryptOptional(string? value, IEncryptionService svc)
        => string.IsNullOrWhiteSpace(value) ? null : await svc.EncryptAsync(value);
}
