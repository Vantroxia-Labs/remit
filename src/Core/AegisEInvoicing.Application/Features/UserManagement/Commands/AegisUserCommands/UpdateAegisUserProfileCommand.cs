using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.AegisUserCommands;

/// <summary>
/// Command to update a Aegis user's profile (platform administrators only)
/// Critical security: Only Aegis platform admins can update Aegis user profiles
/// </summary>
public record UpdateAegisUserProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? AegisEmployeeId,
    string? AegisDepartment) : IRequest<UpdateAegisUserProfileResult>;

public record UpdateAegisUserProfileResult(
    bool IsSuccess,
    string Message)
{
    public static UpdateAegisUserProfileResult Success(string message)
        => new(true, message);
        
    public static UpdateAegisUserProfileResult Failure(string message)
        => new(false, message);
}

public class UpdateAegisUserProfileCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<UpdateAegisUserProfileCommand, UpdateAegisUserProfileResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<UpdateAegisUserProfileResult> Handle(UpdateAegisUserProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated
            if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            {
                return UpdateAegisUserProfileResult.Failure("Authentication required");
            }

            // Step 2: Security validation - verify user is Aegis platform admin
            if (!_currentUser.IsAegisUser || !_currentUser.HasRole(RoleConstants.AegisAdmin))
            {
                return UpdateAegisUserProfileResult.Failure("Only Aegis Platform Admins can update Aegis user profiles");
            }

            // Step 3: Find the Aegis user to update
            var AegisUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsAegisUser, cancellationToken);

            if (AegisUser == null)
            {
                return UpdateAegisUserProfileResult.Failure("Aegis user not found");
            }

            // Step 4: Validate Aegis Employee ID is unique if provided and changed
            if (!string.IsNullOrWhiteSpace(request.AegisEmployeeId) && 
                request.AegisEmployeeId != AegisUser.AegisEmployeeId)
            {
                var existingEmployeeId = await _context.Users
                    .FirstOrDefaultAsync(u => u.AegisEmployeeId == request.AegisEmployeeId && u.Id != request.UserId, 
                        cancellationToken);
                
                if (existingEmployeeId != null)
                {
                    return UpdateAegisUserProfileResult.Failure($"Aegis Employee ID '{request.AegisEmployeeId}' already exists");
                }
            }

            // Step 5: Update basic profile information
            AegisUser.UpdateProfile(request.FirstName, request.LastName, _currentUser.UserId.Value, request.PhoneNumber);

            // Step 6: Update Aegis-specific profile information
            AegisUser.UpdateAegisProfile(request.AegisEmployeeId, request.AegisDepartment, _currentUser.UserId.Value);

            // Step 7: Update activity timestamp
            AegisUser.UpdateAegisActivity();

            // Step 8: Save changes
            await _context.SaveChangesAsync(cancellationToken);

            return UpdateAegisUserProfileResult.Success(
                $"Aegis user '{AegisUser.Email}' profile updated successfully");
        }
        catch (Exception ex)
        {
            return UpdateAegisUserProfileResult.Failure($"Failed to update Aegis user profile: {ex.Message}");
        }
    }
}