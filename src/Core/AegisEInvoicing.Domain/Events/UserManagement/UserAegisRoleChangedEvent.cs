using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Domain.Events.UserManagement;

public record UserAegisRoleChangedEvent(
    Guid UserId,
    string Email,
    AegisRole? OldRole,
    AegisRole NewRole,
    Guid UpdatedBy,
    DateTimeOffset UpdatedAt) : DomainEvent;