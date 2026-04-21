using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetBusinessSubscription;

public class GetBusinessSubscriptionQueryHandler : IRequestHandler<GetBusinessSubscriptionQuery, BusinessSubscriptionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBusinessSubscriptionQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BusinessSubscriptionDto> Handle(GetBusinessSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Businesses.AsQueryable();

        // Apply security filters - Business admins can only see their own business subscription
        if (!_currentUserService.IsPlatformAdmin && _currentUserService.BusinessId.HasValue)
        {
            query = query.Where(b => b.Id == _currentUserService.BusinessId!.Value);
        }

        if (_currentUserService.IsPlatformAdmin && request.BusinessId.HasValue)
        {
            query = query.Where(b => b.Id == request.BusinessId.Value);
        }

        var business = await query
            .Include(b => b.Subscriptions)
            .ThenInclude(s => s.PlatformSubscription)
            .SingleOrDefaultAsync(cancellationToken);

        if (business is null)
            throw new NotFoundException("Business Not Found");

        var primarySub = business.GetPrimarySubscription()
            ?? throw new NotFoundException("Business has no subscription");

        return new BusinessSubscriptionDto(
            primarySub.PlatformSubscription.PlanName,
            primarySub.PlatformSubscription.MonthlyPrice,
            primarySub.Status,
            primarySub.StartDate,
            primarySub.EndDate,
            primarySub.NextBillingDate);
    }
}
