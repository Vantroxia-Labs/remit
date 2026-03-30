using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record PlatformRoleDeactivatedEvent(
    Guid RoleId,
    string RoleName,
    Guid DeactivatedBy,
    DateTimeOffset OccurredAt) : DomainEventBase;