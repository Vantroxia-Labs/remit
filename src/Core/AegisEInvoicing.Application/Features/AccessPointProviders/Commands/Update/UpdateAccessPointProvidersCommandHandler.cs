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

        // Encrypt only if new credentials were supplied; otherwise pass null to keep existing
        var encryptedCredentials = string.IsNullOrWhiteSpace(request.CredentialsJson)
            ? null
            : await encryptionService.EncryptAsync(request.CredentialsJson);

        var encryptedSandbox = string.IsNullOrWhiteSpace(request.SandboxCredentialsJson)
            ? null
            : await encryptionService.EncryptAsync(request.SandboxCredentialsJson);

        config.Update(
            request.Name,
            request.Description,
            request.BaseUrl,
            encryptedCredentials,
            request.SandboxBaseUrl,
            encryptedSandbox);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "AppProviderConfiguration updated: Id={Id}, Vendor={Vendor}",
            config.Id, config.Vendor);

        return new UpdateAccessPointProvidersResult(true, "Access point provider updated successfully.");
    }
}
