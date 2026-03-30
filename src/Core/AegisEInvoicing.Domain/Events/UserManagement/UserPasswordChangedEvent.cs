using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserPasswordChangedEvent(
    Guid UserId,
    Guid? TenantId,
    string Email,
    Guid? ChangedBy,
    bool IsReset,
    DateTimeOffset ChangedAt) : DomainEvent;