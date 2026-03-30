using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record SubscriptionBilledEvent(
    Guid SubscriptionId,
    Guid MerchantId,
    Guid PlatformSubscriptionId,
    DateTimeOffset BillingDate,
    double Amount,
    string Currency,
    Guid BilledBy,
    DateTimeOffset OccurredAt) : DomainEventBase;