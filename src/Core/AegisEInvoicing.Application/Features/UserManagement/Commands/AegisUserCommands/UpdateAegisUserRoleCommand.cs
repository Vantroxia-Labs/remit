using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.AegisUserCommands;

/// <summary>
/// Command to update a Aegis user's role (platform administrators only)
/// Critical security: Only Aegis platform admins can update Aegis user roles
/// </summary>
public record UpdateAegisUserRoleCommand(Guid UserId, AegisRole NewAegisRole) : IRequest<UpdateAegisUserRoleResult>;

public record UpdateAegisUserRoleResult(
    bool IsSuccess,
    string Message)
{
    public static UpdateAegisUserRoleResult Success(string message)
        => new(true, message);
        
    public static UpdateAegisUserRoleResult Failure(string message)
        => new(false, message);
}

public class UpdateAegisUserRoleCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<UpdateAegisUserRoleCommand, UpdateAegisUserRoleResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<UpdateAegisUserRoleResult> Handle(UpdateAegisUserRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated
            if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            {
                return UpdateAegisUserRoleResult.Failure("Authentication required");
            }

            // Step 2: Security validation - verify user is Aegis platform admin
            if (!_currentUser.IsAegisUser || !_currentUser.HasRole(RoleConstants.AegisAdmin))
            {
                return UpdateAegisUserRoleResult.Failure("Only Aegis Platform Admins can update Aegis user roles");
            }

            // Step 3: Find the Aegis user to update
            var AegisUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsAegisUser, cancellationToken);

            if (AegisUser == null)
            {
                return UpdateAegisUserRoleResult.Failure("Aegis user not found");
            }

            // Step 4: Prevent changing own role if downgrading from PlatformAdmin
            if (request.UserId == _currentUser.UserId.Value && 
                AegisUser.AegisRole == AegisRole.AegisAdmin && 
                request.NewAegisRole != AegisRole.AegisAdmin)
            {
                return UpdateAegisUserRoleResult.Failure("Cannot remove your own Platform Admin role");
            }

            // Step 5: Update the Aegis role
            var oldRole = AegisUser.AegisRole;
            AegisUser.UpdateAegisRole(request.NewAegisRole, _currentUser.UserId.Value);

            // Step 6: Update activity timestamp
            AegisUser.UpdateAegisActivity();

            // Step 7: Save changes
            await _context.SaveChangesAsync(cancellationToken);

            return UpdateAegisUserRoleResult.Success(
                $"Aegis user '{AegisUser.Email}' role updated from '{oldRole?.GetDisplayName()}' to '{request.NewAegisRole.GetDisplayName()}'");
        }
        catch (Exception ex)
        {
            return UpdateAegisUserRoleResult.Failure($"Failed to update Aegis user role: {ex.Message}");
        }
    }
}