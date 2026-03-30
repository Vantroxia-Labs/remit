using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.PlatformSubscriptions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.PlatformSubscriptions.Queries.GetPlatformSubscriptionById;

public class GetPlatformSubscriptionByIdQueryHandler : IRequestHandler<GetPlatformSubscriptionByIdQuery, PlatformSubscriptionDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetPlatformSubscriptionByIdQueryHandler> _logger;

    public GetPlatformSubscriptionByIdQueryHandler(
        IApplicationDbContext context,
        ILogger<GetPlatformSubscriptionByIdQueryHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PlatformSubscriptionDto?> Handle(GetPlatformSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var platformSubscription = await _context.PlatformSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == request.PlatformSubscriptionId, cancellationToken);

            if (platformSubscription is null)
                return null;

            // Get subscription count
            var subscriptions = await _context.Subscriptions
                .Where(u => u.PlatformSubscriptionId == request.PlatformSubscriptionId && u.Status == Domain.Entities.BusinessManagement.SubscriptionStatus.Active)
                .CountAsync(cancellationToken);

            return new PlatformSubscriptionDto(platformSubscription.Id,
                platformSubscription.PlanName,
                platformSubscription.Tier,
                platformSubscription.MonthlyPrice,
                platformSubscription.Currency,
                platformSubscription.Description,
                subscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving platform subscription details: {PlatformSubscriptionId}", request.PlatformSubscriptionId);
            throw;
        }
    }
}
