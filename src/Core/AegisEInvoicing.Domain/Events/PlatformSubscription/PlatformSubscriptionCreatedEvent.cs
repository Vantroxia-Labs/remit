using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities;

namespace AegisEInvoicing.Domain.Events.PlatformSubscription;

public record PlatformSubscriptionCreatedEvent(
    Guid SubscriptionId,
    string PlanName,
    SubscriptionTier Tier,
    Guid CreatedBy,
    DateTimeOffset OccurredAt) : DomainEventBase;
