using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record PlatformRoleActivatedEvent(
    Guid RoleId,
    string RoleName,
    Guid ActivatedBy,
    DateTimeOffset OccurredAt) : DomainEventBase;