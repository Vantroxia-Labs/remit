using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserPasswordResetEvent(
    Guid UserId,
    Guid TenantId,
    string Email,
    Guid ResetBy,
    DateTimeOffset ResetAt) : DomainEvent;