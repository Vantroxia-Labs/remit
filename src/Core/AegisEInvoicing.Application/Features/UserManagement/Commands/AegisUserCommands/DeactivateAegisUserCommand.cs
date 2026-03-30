using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.AegisUserCommands;

/// <summary>
/// Command to deactivate a Aegis user (platform administrators only)
/// Critical security: Only Aegis platform admins can deactivate other Aegis users
/// </summary>
public record DeactivateAegisUserCommand(Guid UserId, string Reason) : IRequest<DeactivateAegisUserResult>;

public record DeactivateAegisUserResult(
    bool IsSuccess,
    string Message)
{
    public static DeactivateAegisUserResult Success(string message)
        => new(true, message);
        
    public static DeactivateAegisUserResult Failure(string message)
        => new(false, message);
}

public class DeactivateAegisUserCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<DeactivateAegisUserCommand, DeactivateAegisUserResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<DeactivateAegisUserResult> Handle(DeactivateAegisUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated
            if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            {
                return DeactivateAegisUserResult.Failure("Authentication required");
            }

            // Step 2: Security validation - verify user is Aegis platform admin
            if (!_currentUser.IsAegisUser || !_currentUser.HasRole(RoleConstants.AegisAdmin))
            {
                return DeactivateAegisUserResult.Failure("Only Aegis Platform Admins can deactivate Aegis users");
            }

            // Step 3: Prevent self-deactivation
            if (request.UserId == _currentUser.UserId.Value)
            {
                return DeactivateAegisUserResult.Failure("Cannot deactivate your own account");
            }

            // Step 4: Find the Aegis user to deactivate
            var AegisUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsAegisUser, cancellationToken);

            if (AegisUser == null)
            {
                return DeactivateAegisUserResult.Failure("Aegis user not found");
            }

            // Step 5: Deactivate the user
            AegisUser.Deactivate(_currentUser.UserId.Value, request.Reason);

            // Step 6: Save changes
            await _context.SaveChangesAsync(cancellationToken);

            return DeactivateAegisUserResult.Success(
                $"Aegis user '{AegisUser.Email}' deactivated successfully");
        }
        catch (Exception ex)
        {
            return DeactivateAegisUserResult.Failure($"Failed to deactivate Aegis user: {ex.Message}");
        }
    }
}