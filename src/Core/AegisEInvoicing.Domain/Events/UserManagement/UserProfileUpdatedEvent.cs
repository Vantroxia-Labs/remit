using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserProfileUpdatedEvent(
    Guid UserId,
    Guid? TenantId,
    string Email,
    Guid UpdatedBy,
    Dictionary<string, object> Changes,
    DateTimeOffset UpdatedAt) : DomainEvent;