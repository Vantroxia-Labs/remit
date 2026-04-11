using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Delete;

public class DeleteAccessPointProvidersCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<DeleteAccessPointProvidersCommandHandler> logger)
    : IRequestHandler<DeleteAccessPointProvidersCommand, DeleteAccessPointProvidersResult>
{
    public async Task<DeleteAccessPointProvidersResult> Handle(
        DeleteAccessPointProvidersCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsPlatformAdmin)
            return new DeleteAccessPointProvidersResult(false, "Only AegisAdmin users may manage APP provider configurations.");

        var config = await context.AppProviderConfigurations
            .FirstOrDefaultAsync(p => p.Id == request.configurationId && !p.IsDeleted, cancellationToken);

        if (config is null)
            return new DeleteAccessPointProvidersResult(false, "Access point provider configuration not found.");

        // Soft delete via EF change tracker (ApplicationDbContext handles IsDeleted/DeletedAt/DeletedBy)
        context.AppProviderConfigurations.Remove(config);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "AppProviderConfiguration soft-deleted: Id={Id}, Vendor={Vendor}",
            config.Id, config.Vendor);

        return new DeleteAccessPointProvidersResult(true, "Access point provider deleted successfully.");
    }
}
