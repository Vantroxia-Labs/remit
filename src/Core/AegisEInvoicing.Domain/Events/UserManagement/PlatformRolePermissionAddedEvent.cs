using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record PlatformRolePermissionAddedEvent(
    Guid RoleId,
    string RoleName,
    string Permission,
    DateTimeOffset OccurredAt) : DomainEventBase;