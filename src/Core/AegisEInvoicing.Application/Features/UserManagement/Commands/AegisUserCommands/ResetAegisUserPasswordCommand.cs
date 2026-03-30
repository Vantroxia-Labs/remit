using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.ValueObjects.UserManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.AegisUserCommands;

/// <summary>
/// Command to reset a Aegis user's password (platform administrators only)
/// Critical security: Only Aegis platform admins can reset Aegis user passwords
/// </summary>
public record ResetAegisUserPasswordCommand(Guid UserId, string NewPassword) : IRequest<ResetAegisUserPasswordResult>;

public record ResetAegisUserPasswordResult(
    bool IsSuccess,
    string Message)
{
    public static ResetAegisUserPasswordResult Success(string message)
        => new(true, message);
        
    public static ResetAegisUserPasswordResult Failure(string message)
        => new(false, message);
}

public class ResetAegisUserPasswordCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<ResetAegisUserPasswordCommand, ResetAegisUserPasswordResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<ResetAegisUserPasswordResult> Handle(ResetAegisUserPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated
            if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            {
                return ResetAegisUserPasswordResult.Failure("Authentication required");
            }

            // Step 2: Security validation - verify user is Aegis platform admin
            if (!_currentUser.IsAegisUser || !_currentUser.HasRole(RoleConstants.AegisAdmin))
            {
                return ResetAegisUserPasswordResult.Failure("Only Aegis Platform Admins can reset Aegis user passwords");
            }

            // Step 3: Find the Aegis user to reset password for
            var AegisUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsAegisUser, cancellationToken);

            if (AegisUser == null)
            {
                return ResetAegisUserPasswordResult.Failure("Aegis user not found");
            }

            // Step 4: Create new password hash
            var newPasswordHash = PasswordHash.Create(request.NewPassword);

            // Step 5: Reset the password
            AegisUser.ChangePassword(newPasswordHash, _currentUser.UserId.Value, isReset: true);
            
            // Step 6: Update activity timestamp
            AegisUser.UpdateAegisActivity();

            // Step 7: Save changes
            await _context.SaveChangesAsync(cancellationToken);

            return ResetAegisUserPasswordResult.Success(
                $"Aegis user '{AegisUser.Email}' password reset successfully");
        }
        catch (Exception ex)
        {
            return ResetAegisUserPasswordResult.Failure($"Failed to reset Aegis user password: {ex.Message}");
        }
    }
}