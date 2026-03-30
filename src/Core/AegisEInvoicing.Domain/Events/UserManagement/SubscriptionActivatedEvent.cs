using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record SubscriptionActivatedEvent(
    Guid SubscriptionId,
    Guid MerchantId,
    Guid PlatformSubscriptionId,
    Guid ActivatedBy,
    DateTimeOffset OccurredAt) : DomainEventBase;