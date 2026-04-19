using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.UserManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

/// <summary>
/// Command to assign a role to a user within a merchant
/// Critical security: Only merchant admins can assign roles to users in their own merchant
/// </summary>
public record AssignRoleCommand(
    Guid UserId,
    Guid RoleId,
    DateTimeOffset? ExpiresAt = null) : IRequest<AssignRoleResult>;

public record AssignRoleResult(
    bool IsSuccess,
    string Message)
{
    public static AssignRoleResult Success(string message) => new(true, message);
    public static AssignRoleResult Failure(string message) => new(false, message);
}

public class AssignRoleCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<AssignRoleCommand, AssignRoleResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<AssignRoleResult> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated and has merchant context
            if (!_currentUser.IsAuthenticated || _currentUser.BusinessId == null)
            {
                return AssignRoleResult.Failure("Authentication required");
            }



            // Step 3: Get the target user and verify it belongs to the same tenant
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (targetUser == null)
            {
                return AssignRoleResult.Failure("User not found");
            }

            // Step 4: CRITICAL SECURITY CHECK - Ensure target user belongs to current user's merchant
            if (targetUser.BusinessId != _currentUser.BusinessId!.Value)
            {
                return AssignRoleResult.Failure("Cannot assign roles to users from other merchants");
            }

            // Step 5: Get the platform role — allow system roles (BusinessId = null)
            // or custom roles scoped to the current user's business
            var platformRole = await _context.PlatformRoles
                .FirstOrDefaultAsync(r => r.Id == request.RoleId
                                       && r.IsActive
                                       && !r.IsDeleted
                                       && (r.BusinessId == null || r.BusinessId == _currentUser.BusinessId.Value),
                                    cancellationToken);

            if (platformRole == null)
            {
                return AssignRoleResult.Failure("Platform role not found or is inactive");
            }

            // Step 6: Get the merchant and verify admin relationship
            var merchant = await _context.Businesses
                .FirstOrDefaultAsync(m => m.Id == _currentUser.BusinessId.Value, cancellationToken);

            if (merchant == null || !merchant.CanManageUsers(_currentUser.UserId!.Value))
            {
                return AssignRoleResult.Failure("Only merchant administrators can assign roles");
            }

            // Step 7: Check if user already has this role using domain method
            if (targetUser.HasRole(request.RoleId))
            {
                return AssignRoleResult.Failure($"User already has the role '{platformRole.Name}'");
            }

            // Step 8: Assign role using domain method

            //targetUser.AssignRole(request.RoleId, _currentUser.UserId.Value, request?.ExpiresAt);

            var UserRole = UserRoleAssignment.Create(targetUser.Id, platformRole.Id, _currentUser.UserId.Value, request?.ExpiresAt);
            await _context.UserRoleAssignments.AddAsync(UserRole);
            await _context.SaveChangesAsync(cancellationToken);

            return AssignRoleResult.Success($"Role '{platformRole.Name}' assigned successfully to user '{targetUser.Email}'");
        }
        catch (Exception ex)
        {
            return AssignRoleResult.Failure($"Failed to assign role: {ex.Message}");
        }
    }
}