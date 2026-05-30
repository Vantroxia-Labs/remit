using EInvoiceIntegrator.Application.Common.Interfaces;
using EInvoiceIntegrator.Application.Features.BusinessManagement.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EInvoiceIntegrator.Application.Features.BusinessManagement.Handlers;

public class GetUsageStatsQueryHandler : IRequestHandler<GetUsageStatsQuery, IEnumerable<BusinessUsageStats>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetUsageStatsQueryHandler> _logger;

    public GetUsageStatsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetUsageStatsQueryHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<BusinessUsageStats>> Handle(GetUsageStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var businesses = await _context.Businesses
                .AsNoTracking()
                .Include(b => b.Subscriptions)
                    .ThenInclude(s => s.PlatformSubscription)
                .ToListAsync(cancellationToken);

            var stats = new List<BusinessUsageStats>();

            foreach (var business in businesses)
            {
                var invoiceCount = await _context.Invoices
                    .AsNoTracking()
                    .CountAsync(i => i.BusinessId == business.Id && 
                                    i.CreatedAt >= request.FromDate && i.CreatedAt <= request.ToDate, 
                              cancellationToken);

                var primarySub = business.Subscriptions.FirstOrDefault(s => s.IsActive()) ?? business.Subscriptions.FirstOrDefault();
                stats.Add(new BusinessUsageStats
                {
                    BusinessId = business.Id,
                    BusinessName = business.Name,
                    Period = $"{request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd}",
                    InvoicesProcessed = invoiceCount,
                    SubscriptionTier = primarySub?.PlatformSubscription?.Tier.ToString() ?? "None",
                    IsWithinLimits = true
                });
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage statistics from {FromDate} to {ToDate}", 
                request.FromDate, request.ToDate);
            throw;
        }
    }
}