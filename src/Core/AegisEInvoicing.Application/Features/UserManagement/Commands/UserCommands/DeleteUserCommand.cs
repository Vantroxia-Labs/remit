using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

/// <summary>
/// Command to delete a user within a merchant
/// Critical security: Only merchant admins can delete users in their own merchant
/// </summary>
public record DeleteUserCommand(
    Guid UserId,
    string Reason) : IRequest<DeleteUserResult>;

public record DeleteUserResult(
    bool IsSuccess,
    string Message)
{
    public static DeleteUserResult Success(string message) => new(true, message);
    public static DeleteUserResult Failure(string message) => new(false, message);
}

public class DeleteUserCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<DeleteUserCommand, DeleteUserResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<DeleteUserResult> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated and has merchant context
            if (!_currentUser.IsAuthenticated || _currentUser.BusinessId == null)
            {
                return DeleteUserResult.Failure("Authentication required");
            }

            // Step 2: Security validation - verify user has admin permissions
            if (!_currentUser.HasPermission("user:manage"))
            {
                return DeleteUserResult.Failure("Insufficient permissions to delete users");
            }

            // Step 3: Prevent self-deletion (admin cannot delete themselves)
            if (request.UserId == _currentUser.UserId!.Value)
            {
                return DeleteUserResult.Failure("Cannot delete your own account");
            }

            // Step 4: Get the target user and verify it belongs to the same tenant
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            
            if (targetUser == null)
            {
                return DeleteUserResult.Failure("User not found");
            }

            // Step 5: CRITICAL SECURITY CHECK - Ensure target user belongs to current user's merchant
            if (targetUser.BusinessId != _currentUser.BusinessId.Value)
            {
                return DeleteUserResult.Failure("Cannot delete users from other merchants");
            }

            // Step 6: Get the merchant and verify admin relationship
            var merchant = await _context.Businesses
                .FirstOrDefaultAsync(m => m.Id == _currentUser.BusinessId.Value, cancellationToken);
            
            if (merchant == null || !merchant.CanManageUsers(_currentUser.UserId!.Value))
            {
                return DeleteUserResult.Failure("Only merchant administrators can delete users");
            }

            // Step 7: Mark user as deleted (domain event will be raised)
            targetUser.Delete(_currentUser.UserId.Value, request.Reason);

            // Step 8: Remove user role assignments (cascade delete)
            var userRoleAssignments = await _context.UserRoleAssignments
                .Where(ura => ura.UserId == request.UserId)
                .ToListAsync(cancellationToken);
            
            _context.UserRoleAssignments.RemoveRange(userRoleAssignments);

            // Step 9: Remove user sessions (cascade delete)
            var userSessions = await _context.UserSessions
                .Where(us => us.UserId == request.UserId)
                .ToListAsync(cancellationToken);
            
            _context.UserSessions.RemoveRange(userSessions);

            await _context.SaveChangesAsync(cancellationToken);

            return DeleteUserResult.Success($"User '{targetUser.Email}' deleted successfully");
        }
        catch (Exception ex)
        {
            return DeleteUserResult.Failure($"Failed to delete user: {ex.Message}");
        }
    }
}