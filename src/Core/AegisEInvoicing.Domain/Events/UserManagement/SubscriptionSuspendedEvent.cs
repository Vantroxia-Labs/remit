using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record SubscriptionSuspendedEvent(
    Guid SubscriptionId,
    Guid MerchantId,
    Guid PlatformSubscriptionId,
    Guid SuspendedBy,
    string Reason,
    DateTimeOffset OccurredAt) : DomainEventBase;