using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserActivatedEvent(
    Guid UserId,
    Guid? TenantId,
    string Email,
    Guid ActivatedBy,
    DateTimeOffset ActivatedAt) : DomainEvent;