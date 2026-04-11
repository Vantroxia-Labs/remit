using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
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

        // If a specific vendor is requested, verify an active configuration exists
        if (request.Vendor.HasValue)
        {
            var configExists = await context.AppProviderConfigurations
                .AnyAsync(p => p.Vendor == request.Vendor.Value && p.IsActive && !p.IsDeleted, cancellationToken);

            if (!configExists)
                return new SetBusinessAppProviderResult(false,
                    $"No active APP provider configuration found for vendor '{request.Vendor.Value}'.");
        }

        business.SetVendor(request.Vendor, currentUser.UserId!.Value);
        await context.SaveChangesAsync(cancellationToken);

        var label = request.Vendor.HasValue ? request.Vendor.Value.ToString() : "Interswitch (default)";
        logger.LogInformation(
            "Business {BusinessId} APP vendor set to '{Vendor}' by user {UserId}",
            businessId, label, currentUser.UserId);

        return new SetBusinessAppProviderResult(true, $"APP provider updated to '{label}'.");
    }
}
