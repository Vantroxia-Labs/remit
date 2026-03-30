using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserDeletedEvent(
    Guid UserId,
    Guid? TenantId,
    string Email,
    Guid DeletedBy,
    string Reason,
    DateTimeOffset DeletedAt) : DomainEvent;