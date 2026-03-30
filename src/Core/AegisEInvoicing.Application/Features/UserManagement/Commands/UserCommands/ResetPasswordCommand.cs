using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.ValueObjects.UserManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

/// <summary>
/// Command for admin to reset a user's password within their merchant
/// Critical security: Only merchant admins can reset passwords for users in their own merchant
/// </summary>
public record ResetPasswordCommand(
    Guid UserId,
    string NewPassword) : IRequest<ResetPasswordResult>;

public record ResetPasswordResult(
    bool IsSuccess,
    string Message)
{
    public static ResetPasswordResult Success(string message) => new(true, message);
    public static ResetPasswordResult Failure(string message) => new(false, message);
}

public class ResetPasswordCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<ResetPasswordCommand, ResetPasswordResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<ResetPasswordResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated and has merchant context
            if (!_currentUser.IsAuthenticated || _currentUser.BusinessId == null)
            {
                return ResetPasswordResult.Failure("Authentication required");
            }

            // Step 2: Security validation - verify user has admin permissions
            if (!_currentUser.HasPermission("user:manage"))
            {
                return ResetPasswordResult.Failure("Insufficient permissions to reset passwords");
            }

            // Step 3: Get the target user and verify it belongs to the same tenant
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            
            if (targetUser == null)
            {
                return ResetPasswordResult.Failure("User not found");
            }

            // Step 4: CRITICAL SECURITY CHECK - Ensure target user belongs to current user's merchant
            if (targetUser.BusinessId != _currentUser.BusinessId.Value)
            {
                return ResetPasswordResult.Failure("Cannot reset passwords for users from other merchants");
            }

            // Step 5: Get the merchant and verify admin relationship
            var merchant = await _context.Businesses
                .FirstOrDefaultAsync(m => m.Id == _currentUser.BusinessId.Value, cancellationToken);
            
            if (merchant == null || !merchant.CanManageUsers(_currentUser.UserId!.Value))
            {
                return ResetPasswordResult.Failure("Only merchant administrators can reset passwords");
            }

            // Step 6: Create new password hash
            var newPasswordHash = PasswordHash.Create(request.NewPassword);

            // Step 7: Reset the password (admin-initiated reset)
            targetUser.ChangePassword(newPasswordHash, _currentUser.UserId.Value, isReset: true);
            await _context.SaveChangesAsync(cancellationToken);

            return ResetPasswordResult.Success($"Password reset successfully for user '{targetUser.Email}'");
        }
        catch (Exception ex)
        {
            return ResetPasswordResult.Failure($"Failed to reset password: {ex.Message}");
        }
    }
}