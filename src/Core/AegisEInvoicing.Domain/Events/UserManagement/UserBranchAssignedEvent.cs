using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserBranchAssignedEvent(
    Guid UserId,
    Guid? MerchantId,
    string Email,
    Guid? OldBranchId,
    Guid? NewBranchId,
    Guid AssignedBy,
    DateTimeOffset OccurredAt) : DomainEventBase;