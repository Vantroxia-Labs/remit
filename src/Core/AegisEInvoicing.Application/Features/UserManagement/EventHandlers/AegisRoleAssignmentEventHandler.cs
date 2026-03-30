using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Events.UserManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.UserManagement.EventHandlers;

/// <summary>
/// Handles Aegis role assignment events and assigns appropriate platform roles
/// </summary>
public class AegisRoleAssignmentEventHandler : INotificationHandler<UserAegisRoleChangedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AegisRoleAssignmentEventHandler> _logger;

    public AegisRoleAssignmentEventHandler(
        IApplicationDbContext context,
        ILogger<AegisRoleAssignmentEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(UserAegisRoleChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Only handle Aegis role assignments for users without previous roles (new Aegis users)
            if (notification.OldRole.HasValue)
            {
                _logger.LogInformation("Skipping platform role assignment for Aegis role change from {OldRole} to {NewRole} for user {UserId}", 
                    notification.OldRole, notification.NewRole, notification.UserId);
                return;
            }

            // Find the user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == notification.UserId, cancellationToken);

            if (user is null)
            {
                _logger.LogWarning("User with ID {UserId} not found for Aegis role assignment", notification.UserId);
                return;
            }

            if (!user.IsAegisUser)
            {
                _logger.LogWarning("User {UserId} is not a Aegis user, skipping platform role assignment", notification.UserId);
                return;
            }

            // Get the appropriate platform role based on Aegis role
            var platformRoleName = GetPlatformRoleNameForAegisRole(notification.NewRole);
            var platformRole = await _context.PlatformRoles
                .FirstOrDefaultAsync(r => r.Name == platformRoleName && r.IsActive, cancellationToken);

            if (platformRole == null)
            {
                _logger.LogError("Platform role '{PlatformRoleName}' not found for Aegis role {AegisRole}", 
                    platformRoleName, notification.NewRole);
                return;
            }

            // Check if user already has this platform role
            if (user.HasRole(platformRole.Id))
            {
                _logger.LogInformation("User {UserId} already has platform role {PlatformRoleId}", 
                    user.Id, platformRole.Id);
                return;
            }

            // Assign the platform role
            user.AssignRole(platformRole.Id, notification.UpdatedBy);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully assigned platform role '{PlatformRoleName}' to Aegis user {UserId} with role {AegisRole}",
                platformRoleName, notification.UserId, notification.NewRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Aegis role assignment for user {UserId} with role {AegisRole}", 
                notification.UserId, notification.NewRole);
            throw;
        }
    }

    private static string GetPlatformRoleNameForAegisRole(AegisRole AegisRole)
    {
        return AegisRole switch
        {
            AegisRole.AegisAdmin => "AegisAdmin",
            _ => "AegisAdmin"
        };
    }
}