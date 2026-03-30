using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserLockedOutEvent(
    Guid UserId,
    Guid TenantId,
    string Email,
    string IpAddress,
    int FailedAttempts,
    DateTimeOffset LockedOutAt) : DomainEvent;