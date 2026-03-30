using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserRoleAssignedEvent(
    Guid UserId,
    Guid? MerchantId,
    string Email,
    Guid PlatformRoleId,
    Guid AssignedBy,
    DateTimeOffset OccurredAt) : DomainEventBase;