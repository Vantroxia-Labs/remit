using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserRoleRemovedEvent(
    Guid UserId,
    Guid? MerchantId,
    string Email,
    Guid PlatformRoleId,
    Guid RemovedBy,
    string? Reason,
    DateTimeOffset OccurredAt) : DomainEventBase;