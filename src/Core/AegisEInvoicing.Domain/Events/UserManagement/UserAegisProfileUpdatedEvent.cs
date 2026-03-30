using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserAegisProfileUpdatedEvent(
    Guid UserId,
    string Email,
    Guid UpdatedBy,
    Dictionary<string, object> Changes,
    DateTimeOffset UpdatedAt) : DomainEvent;