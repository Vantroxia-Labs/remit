using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

/// <summary>
/// Command for users to update their own profile or for admins to update user profiles within their business
/// Security: Users can only update their own profile, admins can update any user in their business
/// </summary>
public record UpdateUserProfileCommand(
    Guid? UserId, // If null, updates current user's profile
    string? FirstName,
    string? LastName,
    string? PhoneNumber) : IRequest<UpdateUserProfileResult>;

public record UpdateUserProfileResult(
    bool IsSuccess,
    string Message)
{
    public static UpdateUserProfileResult Success(string message) => new(true, message);
    public static UpdateUserProfileResult Failure(string message) => new(false, message);
}

public class UpdateUserProfileCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<UpdateUserProfileCommand, UpdateUserProfileResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<UpdateUserProfileResult> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated
            if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            {
                return UpdateUserProfileResult.Failure("Authentication required");
            }

            // Step 2: Determine target user ID
            var targetUserId = request.UserId ?? _currentUser.UserId.Value;

            // Step 3: Check if user is trying to update someone else's profile
            var isUpdatingOtherUser = targetUserId != _currentUser.UserId.Value;

            if (isUpdatingOtherUser)
            {
                // Step 4: Security validation - verify user has admin permissions for updating other users
                if (!_currentUser.HasPermission("user:manage"))
                {
                    return UpdateUserProfileResult.Failure("Insufficient permissions to update other user profiles");
                }
            }

            // Step 5: Get the target user
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);
            
            if (targetUser == null)
            {
                return UpdateUserProfileResult.Failure("User not found");
            }

            // Step 6: If updating another user, ensure they belong to the same business
            if (isUpdatingOtherUser)
            {
                if (targetUser.BusinessId != _currentUser.BusinessId!.Value)
                {
                    return UpdateUserProfileResult.Failure("Cannot update users from other businesses");
                }

                // Step 7: Verify admin relationship with business
                var business = await _context.Businesses
                    .FirstOrDefaultAsync(m => m.Id == _currentUser.BusinessId.Value, cancellationToken);
                
                if (business == null || !business.CanManageUsers(_currentUser.UserId!.Value))
                {
                    return UpdateUserProfileResult.Failure("Only business administrators can update other user profiles");
                }
            }

            // Step 8: Determine which fields to update
            var firstName = request.FirstName ?? targetUser.FirstName;
            var lastName = request.LastName ?? targetUser.LastName;
            var phoneNumber = request.PhoneNumber ?? targetUser.PhoneNumber;

            // Step 9: Update the profile
            targetUser.UpdateProfile(firstName, lastName, _currentUser.UserId.Value, phoneNumber);
            await _context.SaveChangesAsync(cancellationToken);

            var message = isUpdatingOtherUser 
                ? $"Profile updated successfully for user '{targetUser.Email}'"
                : "Your profile has been updated successfully";

            return UpdateUserProfileResult.Success(message);
        }
        catch (Exception ex)
        {
            return UpdateUserProfileResult.Failure($"Failed to update profile: {ex.Message}");
        }
    }
}