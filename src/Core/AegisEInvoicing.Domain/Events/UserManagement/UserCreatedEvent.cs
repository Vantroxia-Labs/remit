using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserCreatedEvent(
    Guid UserId,
    Guid? TenantId,
    string Email,
    string FirstName,
    string LastName,
    Guid CreatedBy,
    DateTimeOffset CreatedAt) : DomainEvent;