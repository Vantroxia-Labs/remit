using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserEmailVerifiedEvent(
    Guid UserId,
    Guid TenantId,
    string Email,
    DateTimeOffset VerifiedAt) : DomainEvent;