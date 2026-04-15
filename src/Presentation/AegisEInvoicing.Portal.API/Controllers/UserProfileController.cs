using Asp.Versioning;
using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;
using AegisEInvoicing.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [HttpPost("change-password")]    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
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
    [HttpPut("update")]    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
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
    [RequirePermission(PermissionConstants.UpdateUsers)]    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
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