using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserSuspendedEvent(
    Guid UserId,
    Guid TenantId,
    string Email,
    Guid SuspendedBy,
    string Reason,
    DateTimeOffset SuspendedAt) : DomainEvent;