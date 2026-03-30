using Asp.Versioning;
using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;
using AegisEInvoicing.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for user self-service operations
/// Users can manage their own profiles and passwords
/// </summary>
[ApiVersion("1.0")]
[Authorize] // All profile operations require authentication
[Route("api/v{version:apiVersion}/profile")]
public class UserProfileController : BaseApiController
{
    /// <summary>
    /// Changes the current user's password
    /// </summary>
    /// <param name="request">Password change request with current and new password</param>
    /// <returns>Password change confirmation</returns>
    [HttpPost("change-password")]
    [SwaggerOperation(
        Summary = "Change Password",
        Description = @"Allows authenticated users to change their own password.

**Security Features:**
- Requires current password verification
- New password must meet complexity requirements
- Old password is verified before change
- Password history check (prevents reuse of recent passwords)
- Automatic session invalidation after password change

**Password Requirements:**
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character
- Cannot be the same as current password

**Access Control:**
- **Authentication Required**: Yes
- **Self-Service**: Users can only change their own password

**Example Request:**
```json
{
  ""currentPassword"": ""OldP@ssw0rd"",
  ""newPassword"": ""NewP@ssw0rd123""
}
```

**Example Response:**
```json
{
  ""data"": null,
  ""message"": ""Password changed successfully"",
  ""isSuccess"": true,
  ""statusCode"": 200
}
```"
    )]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var command = new ChangePasswordCommand(request.CurrentPassword, request.NewPassword);
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            return Error(result.Message);
        }

        return Success<object?>(null, result.Message);
    }

    /// <summary>
    /// Updates the current user's profile information
    /// Security: Users can only update their own profile
    /// </summary>
    /// <param name="request">Profile update request</param>
    /// <returns>Profile update result</returns>
    [HttpPut("update")]
    [SwaggerOperation(
        Summary = "Update Own Profile",
        Description = @"Allows authenticated users to update their own profile information.

**Features:**
- Self-service profile updates
- Update first name, last name, and phone number
- Email cannot be changed through this endpoint (requires verification)
- Automatic validation of profile data
- Audit trail for profile changes

**Access Control:**
- **Authentication Required**: Yes
- **Self-Service Only**: Users can only update their own profile
- No administrative privileges required

**Updatable Fields:**
- First name (optional)
- Last name (optional)
- Phone number (optional, must be valid format if provided)

**Non-Updatable Fields:**
- Email (requires separate email change process with verification)
- User ID
- Business/Tenant ID
- Roles and permissions
- Account status

**Validation Rules:**
- Phone number must be valid format if provided (E.164 format recommended: +2348012345678)
- First name and last name must not exceed maximum length
- All fields are optional (null values = no change)

**Example Request:**
```json
{
  ""firstName"": ""John"",
  ""lastName"": ""Doe"",
  ""phoneNumber"": ""+2348012345678""
}
```

**Example Request (Partial Update):**
```json
{
  ""firstName"": ""Jane"",
  ""lastName"": null,
  ""phoneNumber"": null
}
```

**Example Response:**
```json
{
  ""data"": null,
  ""message"": ""Profile updated successfully"",
  ""isSuccess"": true,
  ""statusCode"": 200
}
```"
    )]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var command = new UpdateUserProfileCommand(
            null, // null means update current user's profile
            request.FirstName,
            request.LastName,
            request.PhoneNumber);

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            return Error(result.Message);
        }

        return Success<object?>(null, result.Message);
    }

    /// <summary>
    /// Updates another user's profile (admin-only operation)
    /// Security: Only tenant admins can update other users' profiles within their tenant
    /// </summary>
    /// <param name="userId">User ID to update</param>
    /// <param name="request">Profile update request</param>
    /// <returns>Profile update result</returns>
    [HttpPut("users/{userId}")]
    [RequireClientAdmin]
    [RequirePermission(PermissionConstants.UpdateUsers)]
    [SwaggerOperation(
        Summary = "Update User Profile (Admin)",
        Description = @"Allows business administrators to update profile information for users within their business.

**Features:**
- Administrative profile updates
- Update first name, last name, and phone number for any user in business
- Tenant isolation enforced
- Email cannot be changed through this endpoint (requires verification)
- Automatic validation of profile data
- Audit trail with admin information

**Access Control:**
- **Required Role**: Business Administrator
- **Required Permission**: UpdateUsers
- **Tenant Isolation**: Admins can only update users from their own business
- Cross-tenant profile updates are strictly prohibited

**Updatable Fields:**
- First name (optional)
- Last name (optional)
- Phone number (optional, must be valid format if provided)

**Non-Updatable Fields:**
- Email (requires separate email change process with verification)
- User ID
- Business/Tenant ID
- Roles and permissions (use separate endpoints)
- Account status (use activate/deactivate endpoints)
- Password (use reset password endpoint)

**Use Cases:**
- Correct user information errors
- Update user details on behalf of users
- Administrative profile maintenance
- Bulk profile updates

**Validation Rules:**
- User must belong to admin's business
- Phone number must be valid format if provided (E.164 format recommended: +2348012345678)
- First name and last name must not exceed maximum length
- All fields are optional (null values = no change)

**Example Request:**
```json
{
  ""firstName"": ""John"",
  ""lastName"": ""Doe"",
  ""phoneNumber"": ""+2348012345678""
}
```

**Example Request (Partial Update):**
```json
{
  ""firstName"": ""Jane"",
  ""lastName"": null,
  ""phoneNumber"": null
}
```

**Example Response:**
```json
{
  ""data"": null,
  ""message"": ""User profile updated successfully"",
  ""isSuccess"": true,
  ""statusCode"": 200
}
```"
    )]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateUserProfile([FromRoute] Guid userId, [FromBody] UpdateProfileRequest request)
    {
        var command = new UpdateUserProfileCommand(
            userId,
            request.FirstName,
            request.LastName,
            request.PhoneNumber);

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            var statusCode = result.Message.Contains("permissions") || result.Message.Contains("admin") || result.Message.Contains("other tenants")
                ? StatusCodes.Status403Forbidden 
                : StatusCodes.Status400BadRequest;
            return Error(result.Message, statusCode);
        }

        return Success<object?>(null, result.Message);
    }
}

// Request models
public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber);