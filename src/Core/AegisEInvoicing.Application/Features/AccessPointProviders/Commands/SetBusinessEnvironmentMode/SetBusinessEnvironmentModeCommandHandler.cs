using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.SetBusinessEnvironmentMode;

public class SetBusinessEnvironmentModeCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<SetBusinessEnvironmentModeCommandHandler> logger)
    : IRequestHandler<SetBusinessEnvironmentModeCommand, SetBusinessEnvironmentModeResult>
{
    public async Task<SetBusinessEnvironmentModeResult> Handle(
        SetBusinessEnvironmentModeCommand request,
        CancellationToken cancellationToken)
    {
        // AegisAdmin can change any business; ClientAdmin can only change their own
        var businessId = currentUser.IsPlatformAdmin
            ? request.BusinessId
            : currentUser.BusinessId ?? Guid.Empty;

        if (businessId == Guid.Empty || (!currentUser.IsPlatformAdmin && businessId != request.BusinessId))
            return new SetBusinessEnvironmentModeResult(false, "Insufficient permissions to modify this business.");

        var business = await context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId && !b.IsDeleted, cancellationToken);

        if (business is null)
            return new SetBusinessEnvironmentModeResult(false, "Business not found.");

        business.SetEnvironmentMode(request.EnvironmentMode, currentUser.UserId!.Value);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Business {BusinessId} environment mode set to '{Mode}' by user {UserId}",
            businessId, request.EnvironmentMode, currentUser.UserId);

        return new SetBusinessEnvironmentModeResult(true,
            $"Environment mode updated to '{request.EnvironmentMode}'.");
    }
}
