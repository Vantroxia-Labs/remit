using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserLoginFailedEvent(
    Guid UserId,
    Guid TenantId,
    string Email,
    string IpAddress,
    int FailedAttempts,
    DateTimeOffset AttemptedAt) : DomainEvent;