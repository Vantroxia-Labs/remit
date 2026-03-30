using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

/// <summary>
/// Command to deactivate a user within a merchant
/// Critical security: Only merchant admins can deactivate users in their own merchant
/// </summary>
public record DeactivateUserCommand(
    Guid UserId,
    string Reason) : IRequest<DeactivateUserResult>;

public record DeactivateUserResult(
    bool IsSuccess,
    string Message)
{
    public static DeactivateUserResult Success(string message) => new(true, message);
    public static DeactivateUserResult Failure(string message) => new(false, message);
}

public class DeactivateUserCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<DeactivateUserCommand, DeactivateUserResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<DeactivateUserResult> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated and has merchant context
            if (!_currentUser.IsAuthenticated || _currentUser.BusinessId == null)
            {
                return DeactivateUserResult.Failure("Authentication required");
            }

            // Step 2: Security validation - verify user has admin permissions
            //if (!_currentUser.HasPermission("user:manage"))
            //{
            //    return DeactivateUserResult.Failure("Insufficient permissions to deactivate users");
            //}

            // Step 3: Prevent self-deactivation (admin cannot deactivate themselves)
            if (request.UserId == _currentUser.UserId!.Value)
            {
                return DeactivateUserResult.Failure("Cannot deactivate your own account");
            }

            // Step 4: Get the target user and verify it belongs to the same tenant
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            
            if (targetUser == null)
            {
                return DeactivateUserResult.Failure("User not found");
            }

            // Step 5: CRITICAL SECURITY CHECK - Ensure target user belongs to current user's merchant
            if (targetUser.BusinessId != _currentUser.BusinessId.Value)
            {
                return DeactivateUserResult.Failure("Cannot deactivate users from other merchants");
            }

            // Step 6: Get the merchant and verify admin relationship
            var merchant = await _context.Businesses
                .FirstOrDefaultAsync(m => m.Id == _currentUser.BusinessId.Value, cancellationToken);
            
            if (merchant == null || !merchant.CanManageUsers(_currentUser.UserId!.Value))
            {
                return DeactivateUserResult.Failure("Only merchant administrators can deactivate users");
            }

            // Step 7: Deactivate the user
            targetUser.Deactivate(_currentUser.UserId.Value, request.Reason);
            await _context.SaveChangesAsync(cancellationToken);

            return DeactivateUserResult.Success($"User '{targetUser.Email}' deactivated successfully");
        }
        catch (Exception ex)
        {
            return DeactivateUserResult.Failure($"Failed to deactivate user: {ex.Message}");
        }
    }
}