using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserBranchRemovedEvent(
    Guid UserId,
    Guid? MerchantId,
    string Email,
    Guid OldBranchId,
    Guid RemovedBy,
    DateTimeOffset OccurredAt) : DomainEventBase;