using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

/// <summary>
/// Command to revoke a role from a user within a merchant
/// Critical security: Only merchant admins can revoke roles from users in their own merchant
/// </summary>
public record RevokeRoleCommand(
    Guid UserId,
    Guid RoleId,
    string? Reason = null) : IRequest<RevokeRoleResult>;

public record RevokeRoleResult(
    bool IsSuccess,
    string Message)
{
    public static RevokeRoleResult Success(string message) => new(true, message);
    public static RevokeRoleResult Failure(string message) => new(false, message);
}

public class RevokeRoleCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<RevokeRoleCommand, RevokeRoleResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<RevokeRoleResult> Handle(RevokeRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated and has merchant context
            if (!_currentUser.IsAuthenticated || _currentUser.BusinessId == null)
            {
                return RevokeRoleResult.Failure("Authentication required");
            }

            // Step 2: Security validation - verify user has role management permissions
            if (!_currentUser.HasPermission("role:manage"))
            {
                return RevokeRoleResult.Failure("Insufficient permissions to revoke roles");
            }

            // Step 3: Get the target user and verify it belongs to the same tenant
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            
            if (targetUser == null)
            {
                return RevokeRoleResult.Failure("User not found");
            }

            // Step 4: CRITICAL SECURITY CHECK - Ensure target user belongs to current user's merchant
            if (targetUser.BusinessId != _currentUser.BusinessId!.Value)
            {
                return RevokeRoleResult.Failure("Cannot revoke roles from users in other merchants");
            }

            // Step 5: Get the platform role
            var platformRole = await _context.PlatformRoles
                .FirstOrDefaultAsync(r => r.Id == request.RoleId && r.IsActive, cancellationToken);
            
            if (platformRole == null)
            {
                return RevokeRoleResult.Failure("Platform role not found or is inactive");
            }

            // Step 6: Get the merchant and verify admin relationship
            var merchant = await _context.Businesses
                .FirstOrDefaultAsync(m => m.Id == _currentUser.BusinessId.Value, cancellationToken);
            
            if (merchant == null || !merchant.CanManageUsers(_currentUser.UserId!.Value))
            {
                return RevokeRoleResult.Failure("Only merchant administrators can revoke roles");
            }

            // Step 7: Check if user has this role using domain method
            if (!targetUser.HasRole(request.RoleId))
            {
                return RevokeRoleResult.Failure($"User does not have the role '{platformRole.Name}' or it's already revoked");
            }

            // Step 8: Prevent revoking admin role from merchant admin (would break merchant isolation)
            if (platformRole.Name == "Admin" && merchant.IsOwner(targetUser.Id))
            {
                return RevokeRoleResult.Failure("Cannot revoke admin role from merchant owner");
            }

            // Step 9: Revoke the role using domain method
            targetUser.RevokeRole(request.RoleId, _currentUser.UserId.Value, request.Reason);
            await _context.SaveChangesAsync(cancellationToken);

            return RevokeRoleResult.Success($"Role '{platformRole.Name}' revoked successfully from user '{targetUser.Email}'");
        }
        catch (Exception ex)
        {
            return RevokeRoleResult.Failure($"Failed to revoke role: {ex.Message}");
        }
    }
}