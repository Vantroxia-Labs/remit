using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetSubscriptionPlans;

public record GetSubscriptionPlansQuery : IRequest<IEnumerable<SubscriptionPlanDto>>;

public record SubscriptionPlanDto(
    Guid Id,
    string PlanName,
    string Tier,
    double MonthlyPrice,
    double AnnualPrice,
    string Currency,
    string Description);

public class GetSubscriptionPlansQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetSubscriptionPlansQuery, IEnumerable<SubscriptionPlanDto>>
{
    public async Task<IEnumerable<SubscriptionPlanDto>> Handle(
        GetSubscriptionPlansQuery request,
        CancellationToken cancellationToken)
    {
        var plans = await context.PlatformSubscriptions
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.MonthlyPrice)
            .ToListAsync(cancellationToken);

        return plans.Select(p => new SubscriptionPlanDto(
            p.Id,
            p.PlanName,
            p.Tier.ToString(),
            p.MonthlyPrice,
            p.AnnualPrice,
            p.Currency,
            p.Description));
    }
}
