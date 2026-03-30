using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserLoginSuccessfulEvent(
    Guid UserId,
    Guid TenantId,
    string Email,
    string IpAddress,
    DateTimeOffset LoginAt) : DomainEvent;