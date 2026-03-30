using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries;

public record GetExpiredSubscriptionsQuery : IRequest<IEnumerable<ExpiredSubscriptionDto>>
{
}

public record ExpiredSubscriptionDto
{
    public Guid BusinessId { get; init; }
    public string BusinessName { get; init; } = default!;
    public DateTimeOffset EndDate { get; init; }
    public int DaysOverdue { get; init; }
    public string ContactEmail { get; init; } = default!;
}