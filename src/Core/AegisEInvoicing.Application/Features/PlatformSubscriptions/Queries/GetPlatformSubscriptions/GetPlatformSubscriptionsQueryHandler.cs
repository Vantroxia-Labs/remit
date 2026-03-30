using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.PlatformSubscriptions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.PlatformSubscriptions.Queries.GetPlatformSubscriptions;

public class GetPlatformSubscriptionsQueryHandler 
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPlatformSubscriptionsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<PlatformSubscriptionDto>> Handle(CancellationToken cancellationToken)
    {
        var query = _context.PlatformSubscriptions
            .AsNoTracking()
            .Include(s => s.Subscriptions)
            .AsQueryable();

        var totalCount = await query.CountAsync(cancellationToken);

        var planSubscriptions = await query
            .Select(u => new PlatformSubscriptionDto(
                u.Id, 
                u.PlanName,
                u.Tier,
                u.MonthlyPrice,
                u.Currency,
                u.Description,
                u.Subscriptions.Count()))
            .ToListAsync(cancellationToken);

        return planSubscriptions;
    }
}
