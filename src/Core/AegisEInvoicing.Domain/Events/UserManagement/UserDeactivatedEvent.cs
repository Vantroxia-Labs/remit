using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserDeactivatedEvent(
    Guid UserId,
    Guid? TenantId,
    string Email,
    Guid DeactivatedBy,
    string Reason,
    DateTimeOffset DeactivatedAt) : DomainEvent;