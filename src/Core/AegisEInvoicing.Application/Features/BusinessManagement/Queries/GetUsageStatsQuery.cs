using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries;

public record GetUsageStatsQuery : IRequest<IEnumerable<BusinessUsageStats>>
{
    public DateTimeOffset FromDate { get; init; }
    public DateTimeOffset ToDate { get; init; }
}

public record BusinessUsageStats
{
    public Guid BusinessId { get; init; }
    public string BusinessName { get; init; } = default!;
    public string Period { get; init; } = default!;
    public int InvoicesProcessed { get; init; }
    public string SubscriptionTier { get; init; } = default!;
    public bool IsWithinLimits { get; init; }
}