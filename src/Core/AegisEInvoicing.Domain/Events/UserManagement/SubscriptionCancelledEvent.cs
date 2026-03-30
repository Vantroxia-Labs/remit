using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record SubscriptionCancelledEvent(
    Guid SubscriptionId,
    Guid MerchantId,
    Guid PlatformSubscriptionId,
    Guid CancelledBy,
    string Reason,
    DateTimeOffset OccurredAt) : DomainEventBase;