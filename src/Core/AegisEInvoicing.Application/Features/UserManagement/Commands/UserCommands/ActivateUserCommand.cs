using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

/// <summary>
/// Command to activate a user within a merchant/branch
/// Critical security: Only merchant/branch admins can activate users in their scope
/// </summary>
public record ActivateUserCommand(Guid UserId) : IRequest<ActivateUserResult>;

public record ActivateUserResult(
    bool IsSuccess,
    string Message)
{
    public static ActivateUserResult Success(string message) => new(true, message);
    public static ActivateUserResult Failure(string message) => new(false, message);
}

public class ActivateUserCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<ActivateUserCommand, ActivateUserResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<ActivateUserResult> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated and has merchant context
            if (!_currentUser.IsAuthenticated || _currentUser.BusinessId == null)
            {
                return ActivateUserResult.Failure("Authentication required");
            }

            // Step 2: Security validation - verify user has admin permissions
            //if (!_currentUser.HasPermission("user:manage"))
            //{
            //    return ActivateUserResult.Failure("Insufficient permissions to activate users");
            //}

            // Step 3: Get the target user and verify it belongs to the same tenant
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            
            if (targetUser == null)
            {
                return ActivateUserResult.Failure("User not found");
            }

            // Step 4: CRITICAL SECURITY CHECK - Ensure target user belongs to current user's merchant
            if (targetUser.BusinessId != _currentUser.BusinessId.Value)
            {
                return ActivateUserResult.Failure("Cannot activate users from other merchants");
            }

            // Step 5: Get the merchant and verify admin relationship
            var merchant = await _context.Businesses
                .FirstOrDefaultAsync(m => m.Id == _currentUser.BusinessId.Value, cancellationToken);
            
            if (merchant == null || !merchant.CanManageUsers(_currentUser.UserId!.Value, targetUser.BranchId))
            {
                return ActivateUserResult.Failure("Insufficient permissions to activate this user");
            }

            // Step 6: Activate the user
            targetUser.Activate(_currentUser.UserId.Value);
            await _context.SaveChangesAsync(cancellationToken);

            return ActivateUserResult.Success($"User '{targetUser.Email}' activated successfully");
        }
        catch (Exception ex)
        {
            return ActivateUserResult.Failure($"Failed to activate user: {ex.Message}");
        }
    }
}