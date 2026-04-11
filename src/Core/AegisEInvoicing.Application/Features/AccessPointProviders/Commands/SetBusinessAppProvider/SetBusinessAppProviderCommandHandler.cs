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

        var normalizedCode = string.IsNullOrWhiteSpace(request.ProviderCode)
            ? null
            : request.ProviderCode.ToLowerInvariant().Trim();

        // If a specific provider code is requested, verify it exists in the DB
        if (normalizedCode is not null)
        {
            var providerExists = await context.AppProviderConfigurations
                .AnyAsync(p => p.ProviderCode == normalizedCode && p.IsActive && !p.IsDeleted, cancellationToken);

            if (!providerExists)
                return new SetBusinessAppProviderResult(false,
                    $"No active APP provider configuration found for code '{normalizedCode}'.");
        }

        business.SetAppProvider(normalizedCode, currentUser.UserId!.Value);
        await context.SaveChangesAsync(cancellationToken);

        var label = normalizedCode ?? "interswitch (default)";
        logger.LogInformation(
            "Business {BusinessId} APP provider set to '{Code}' by user {UserId}",
            businessId, label, currentUser.UserId);

        return new SetBusinessAppProviderResult(true, $"APP provider updated to '{label}'.");
    }
}
