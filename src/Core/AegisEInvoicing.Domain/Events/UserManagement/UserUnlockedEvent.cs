using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserUnlockedEvent(
    Guid UserId,
    Guid TenantId,
    string Email,
    Guid UnlockedBy,
    DateTimeOffset UnlockedAt) : DomainEvent;