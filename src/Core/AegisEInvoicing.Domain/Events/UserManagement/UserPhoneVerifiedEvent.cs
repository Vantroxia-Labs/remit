using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserPhoneVerifiedEvent(
    Guid UserId,
    Guid TenantId,
    string PhoneNumber,
    DateTimeOffset VerifiedAt) : DomainEvent;