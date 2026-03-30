using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.PlatformSubscriptions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.PlatformSubscriptions.Queries.GetAllPlatformSubscriptions;

public class GetAllPlatformSubscriptionsQueryHandler : IRequestHandler<GetAllPlatformSubscriptionsQuery, List<PlatformSubscriptionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllPlatformSubscriptionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PlatformSubscriptionDto>> Handle(GetAllPlatformSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var platformSubscriptions = await _context.PlatformSubscriptions
            .AsNoTracking()
            .Include(s => s.Subscriptions)
            .Where(ps => !ps.IsDeleted)
            .OrderBy(ps => ps.PlanName)
            .Select(ps => new PlatformSubscriptionDto(
                ps.Id,
                ps.PlanName,
                ps.Tier,
                ps.MonthlyPrice,
                ps.Currency,
                ps.Description,
                ps.Subscriptions.Count()))
            .ToListAsync(cancellationToken);

        return platformSubscriptions;
    }
}
