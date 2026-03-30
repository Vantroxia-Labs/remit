using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record SubscriptionCreatedEvent(
    Guid SubscriptionId,
    Guid MerchantId,
    Guid platformSubscriptionId,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    Guid CreatedBy,
    DateTimeOffset OccurredAt) : DomainEventBase;