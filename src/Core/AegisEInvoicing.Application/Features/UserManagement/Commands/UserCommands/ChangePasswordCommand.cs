using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.ValueObjects.UserManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

/// <summary>
/// Command for users to change their own password
/// Security: Users can only change their own password, not others
/// </summary>
public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest<ChangePasswordResult>;

public record ChangePasswordResult(
    bool IsSuccess,
    string Message)
{
    public static ChangePasswordResult Success(string message) => new(true, message);
    public static ChangePasswordResult Failure(string message) => new(false, message);
}

public class ChangePasswordCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<ChangePasswordCommand, ChangePasswordResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<ChangePasswordResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated
            if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            {
                return ChangePasswordResult.Failure("Authentication required");
            }

            // Step 2: Get the current user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, cancellationToken);
            
            if (user == null)
            {
                return ChangePasswordResult.Failure("User not found");
            }

            // Step 3: Verify current password
            if (user.PasswordHash.Verify(request.CurrentPassword))
            {
                return ChangePasswordResult.Failure("Current password is incorrect");
            }

            // Step 4: Create new password hash
            var newPasswordHash = PasswordHash.Create(request.NewPassword);

            // Step 5: Change the password (user-initiated change)
            user.ChangePassword(newPasswordHash, _currentUser.UserId.Value, isReset: false);
            await _context.SaveChangesAsync(cancellationToken);

            return ChangePasswordResult.Success("Password changed successfully");
        }
        catch (ArgumentException ex)
        {
            // Password validation errors
            return ChangePasswordResult.Failure($"Password validation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ChangePasswordResult.Failure($"Failed to change password: {ex.Message}");
        }
    }
}