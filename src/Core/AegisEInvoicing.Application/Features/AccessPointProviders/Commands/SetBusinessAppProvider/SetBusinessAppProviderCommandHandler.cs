using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.SetBusinessAppProvider;

public class SetBusinessAppProviderCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<SetBusinessAppProviderCommandHandler> logger)
    : IRequestHandler<SetBusinessAppProviderCommand, SetBusinessAppProviderResult>
{
    public async Task<SetBusinessAppProviderResult> Handle(
        SetBusinessAppProviderCommand request,
        CancellationToken cancellationToken)
    {
        // AegisAdmin can change any business; ClientAdmin can only change their own
        var businessId = currentUser.IsPlatformAdmin
            ? request.BusinessId
            : currentUser.BusinessId ?? Guid.Empty;

        if (businessId == Guid.Empty || (!currentUser.IsPlatformAdmin && businessId != request.BusinessId))
            return new SetBusinessAppProviderResult(false, "Insufficient permissions to modify this business.");

        var business = await context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId && !b.IsDeleted, cancellationToken);

        if (business is null)
            return new SetBusinessAppProviderResult(false, "Business not found.");

        // If a specific adapter is requested, verify an active configuration exists for it
        if (!string.IsNullOrWhiteSpace(request.AdapterKey))
        {
            var normalizedKey = request.AdapterKey.Trim().ToLowerInvariant();

            var configExists = await context.AppProviderConfigurations
                .AnyAsync(p => p.AdapterKey == normalizedKey && p.IsActive && !p.IsDeleted, cancellationToken);

            if (!configExists)
                return new SetBusinessAppProviderResult(false,
                    $"No active APP provider configuration found for adapter '{normalizedKey}'.");

            business.SetAdapterKey(normalizedKey, currentUser.UserId!.Value);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Business {BusinessId} APP adapter set to '{AdapterKey}' by user {UserId}",
                businessId, normalizedKey, currentUser.UserId);

            return new SetBusinessAppProviderResult(true, $"APP provider updated to '{normalizedKey}'.");
        }

        // Null/empty resets to platform default
        business.SetAdapterKey(null, currentUser.UserId!.Value);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Business {BusinessId} APP adapter reset to platform default by user {UserId}",
            businessId, currentUser.UserId);

        return new SetBusinessAppProviderResult(true, "APP provider reset to platform default.");
    }
}
