using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record PlatformRolePermissionRemovedEvent(
    Guid RoleId,
    string RoleName,
    string Permission,
    DateTimeOffset OccurredAt) : DomainEventBase;