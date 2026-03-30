using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities;

namespace AegisEInvoicing.Domain.Events.PlatformSubscription;

/// <summary>
/// Domain event raised when a platform subscription is updated
/// </summary>
public sealed record PlatformSubscriptionUpdatedEvent(
    Guid PlatformSubscriptionId,
    string NewPlanName,
    SubscriptionTier NewTier,
    double NewMonthlyPrice,
    string NewCurrency,
    Guid UpdatedBy,
    DateTimeOffset UpdatedAt,
    string OldPlanName,
    SubscriptionTier OldTier,
    double OldMonthlyPrice,
    string OldCurrency
) : DomainEventBase;
