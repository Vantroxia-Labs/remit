using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserEmailUpdatedEvent(
    Guid UserId,
    Guid TenantId,
    string OldEmail,
    string NewEmail,
    Guid UpdatedBy,
    DateTimeOffset UpdatedAt) : DomainEvent;