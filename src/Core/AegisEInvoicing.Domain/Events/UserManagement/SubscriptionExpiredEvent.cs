using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record SubscriptionExpiredEvent(
    Guid SubscriptionId,
    Guid MerchantId,
    Guid PlatformSubscriptionId,
    Guid ExpiredBy,
    DateTimeOffset OccurredAt) : DomainEventBase;