using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record SubscriptionRenewedEvent(
    Guid SubscriptionId,
    Guid MerchantId,
    Guid PlatformSubscriptionId,
    DateTimeOffset OldEndDate,
    DateTimeOffset NewEndDate,
    Guid RenewedBy,
    DateTimeOffset OccurredAt) : DomainEventBase;