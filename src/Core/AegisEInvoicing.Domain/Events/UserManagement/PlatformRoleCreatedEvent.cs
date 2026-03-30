using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record PlatformRoleCreatedEvent(
    Guid RoleId,
    string RoleName,
    string Description,
    string Category,
    Guid CreatedBy,
    DateTimeOffset OccurredAt) : DomainEventBase;