using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities;

namespace AegisEInvoicing.Domain.Events.PlatformSubscription;

/// <summary>
/// Domain event raised when a platform subscription is soft deleted
/// </summary>
public sealed record PlatformSubscriptionDeletedEvent(
    Guid PlatformSubscriptionId,
    string PlanName,
    SubscriptionTier Tier,
    Guid DeletedBy,
    DateTimeOffset DeletedAt,
    int AffectedSubscriptionsCount
) : DomainEventBase;
