using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.AegisUserCommands;

/// <summary>
/// Command to delete a Aegis user (platform administrators only)
/// Critical security: Only Aegis platform admins can delete other Aegis users
/// </summary>
public record DeleteAegisUserCommand(Guid UserId, string Reason) : IRequest<DeleteAegisUserResult>;

public record DeleteAegisUserResult(
    bool IsSuccess,
    string Message)
{
    public static DeleteAegisUserResult Success(string message)
        => new(true, message);
        
    public static DeleteAegisUserResult Failure(string message)
        => new(false, message);
}

public class DeleteAegisUserCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<DeleteAegisUserCommand, DeleteAegisUserResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<DeleteAegisUserResult> Handle(DeleteAegisUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated
            if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            {
                return DeleteAegisUserResult.Failure("Authentication required");
            }

            // Step 2: Security validation - verify user is Aegis platform admin
            if (!_currentUser.IsAegisUser || !_currentUser.HasRole(RoleConstants.AegisAdmin))
            {
                return DeleteAegisUserResult.Failure("Only Aegis Platform Admins can delete Aegis users");
            }

            // Step 3: Prevent self-deletion
            if (request.UserId == _currentUser.UserId.Value)
            {
                return DeleteAegisUserResult.Failure("Cannot delete your own account");
            }

            // Step 4: Find the Aegis user to delete
            var AegisUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsAegisUser, cancellationToken);

            if (AegisUser == null)
            {
                return DeleteAegisUserResult.Failure("Aegis user not found");
            }

            // Step 5: Check if this is the last PlatformAdmin
            if (AegisUser.AegisRole == AegisRole.AegisAdmin)
            {
                var platformAdminCount = await _context.Users
                    .CountAsync(u => u.IsAegisUser && u.AegisRole == AegisRole.AegisAdmin && u.Id != request.UserId, 
                        cancellationToken);

                if (platformAdminCount == 0)
                {
                    return DeleteAegisUserResult.Failure("Cannot delete the last Platform Admin user");
                }
            }

            // Step 6: Mark user for deletion (soft delete through domain event)
            AegisUser.Delete(_currentUser.UserId.Value, request.Reason);

            // Step 7: Save changes
            await _context.SaveChangesAsync(cancellationToken);

            return DeleteAegisUserResult.Success(
                $"Aegis user '{AegisUser.Email}' deleted successfully");
        }
        catch (Exception ex)
        {
            return DeleteAegisUserResult.Failure($"Failed to delete Aegis user: {ex.Message}");
        }
    }
}