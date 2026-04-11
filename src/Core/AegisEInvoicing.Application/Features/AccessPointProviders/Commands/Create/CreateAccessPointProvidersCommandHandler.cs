using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
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

        var normalizedKey = request.AdapterKey.Trim().ToLowerInvariant();

        var exists = await context.AppProviderConfigurations
            .AnyAsync(p => p.AdapterKey == normalizedKey && !p.IsDeleted, cancellationToken);

        if (exists)
            return new CreateAccessPointProvidersResult(false,
                $"A configuration for adapter '{normalizedKey}' already exists.");

        var encryptedCredentials = await EncryptOptional(request.CredentialsJson, encryptionService);
        var encryptedSandbox     = await EncryptOptional(request.SandboxCredentialsJson, encryptionService);

        var configuration = AppProviderConfiguration.Create(
            request.Name,
            request.Description,
            normalizedKey,
            request.BaseUrl,
            encryptedCredentials,
            request.SandboxBaseUrl,
            encryptedSandbox);

        await context.AppProviderConfigurations.AddAsync(configuration, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "AppProviderConfiguration created: AdapterKey={AdapterKey}, Name={Name}",
            normalizedKey, request.Name);

        return new CreateAccessPointProvidersResult(true, "Access point provider created successfully.", configuration.Id);
    }

    private static async Task<string?> EncryptOptional(string? value, IEncryptionService svc)
        => string.IsNullOrWhiteSpace(value) ? null : await svc.EncryptAsync(value);
}
