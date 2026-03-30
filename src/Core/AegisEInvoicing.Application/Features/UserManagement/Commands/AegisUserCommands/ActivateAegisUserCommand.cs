using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.AegisUserCommands;

/// <summary>
/// Command to activate a Aegis user (platform administrators only)
/// Critical security: Only Aegis platform admins can activate other Aegis users
/// </summary>
public record ActivateAegisUserCommand(Guid UserId) : IRequest<ActivateAegisUserResult>;

public record ActivateAegisUserResult(
    bool IsSuccess,
    string Message)
{
    public static ActivateAegisUserResult Success(string message)
        => new(true, message);
        
    public static ActivateAegisUserResult Failure(string message)
        => new(false, message);
}

public class ActivateAegisUserCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<ActivateAegisUserCommand, ActivateAegisUserResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<ActivateAegisUserResult> Handle(ActivateAegisUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated
            if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            {
                return ActivateAegisUserResult.Failure("Authentication required");
            }

            // Step 2: Security validation - verify user is Aegis platform admin
            if (!_currentUser.IsAegisUser || !_currentUser.HasRole(RoleConstants.AegisAdmin))
            {
                return ActivateAegisUserResult.Failure("Only Aegis Platform Admins can activate Aegis users");
            }

            // Step 3: Find the Aegis user to activate
            var AegisUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsAegisUser, cancellationToken);

            if (AegisUser == null)
            {
                return ActivateAegisUserResult.Failure("Aegis user not found");
            }

            // Step 4: Activate the user
            AegisUser.Activate(_currentUser.UserId.Value);

            // Step 5: Save changes
            await _context.SaveChangesAsync(cancellationToken);

            return ActivateAegisUserResult.Success(
                $"Aegis user '{AegisUser.Email}' activated successfully");
        }
        catch (Exception ex)
        {
            return ActivateAegisUserResult.Failure($"Failed to activate Aegis user: {ex.Message}");
        }
    }
}