using AegisEInvoicing.Domain.Entities.BusinessManagement;

namespace AegisEInvoicing.Application.Features.BusinessManagement.DTOs;

public record BusinessSubscriptionInfo
{
    public string PlanInformation { get; init; } = string.Empty;
    public SubscriptionStatus SubscriptionStatus { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset EndDate { get; init; }
    public bool IsActive { get; init; }
}
