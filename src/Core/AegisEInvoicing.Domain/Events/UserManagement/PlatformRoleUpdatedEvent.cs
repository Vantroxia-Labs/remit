using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record PlatformRoleUpdatedEvent(
    Guid RoleId,
    string RoleName,
    Guid UpdatedBy,
    Dictionary<string, object> Changes,
    DateTimeOffset OccurredAt) : DomainEventBase;