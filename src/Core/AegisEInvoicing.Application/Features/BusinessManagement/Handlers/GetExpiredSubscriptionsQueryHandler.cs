using EInvoiceIntegrator.Application.Common.Interfaces;
using EInvoiceIntegrator.Application.Features.BusinessManagement.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EInvoiceIntegrator.Application.Features.BusinessManagement.Handlers;

public class GetExpiredSubscriptionsQueryHandler : IRequestHandler<GetExpiredSubscriptionsQuery, IEnumerable<ExpiredSubscriptionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetExpiredSubscriptionsQueryHandler> _logger;

    public GetExpiredSubscriptionsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetExpiredSubscriptionsQueryHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<ExpiredSubscriptionDto>> Handle(GetExpiredSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            
            var expiredBusinesses = await _context.Businesses
                .AsNoTracking()
                .Include(b => b.Subscription)
                .Where(b => b.Subscription.EndDate < now && b.Subscription.IsActive())
                .ToListAsync(cancellationToken);

            return expiredBusinesses.Select(b => new ExpiredSubscriptionDto
            {
                BusinessId = b.Id,
                BusinessName = b.Name,
                EndDate = b.Subscription.EndDate,
                DaysOverdue = (int)(now - b.Subscription.EndDate).TotalDays,
                ContactEmail = b.ContactEmail
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expired subscriptions");
            throw;
        }
    }
}