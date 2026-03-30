using Asp.Versioning;
using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Features.UserManagement.Commands.AegisUserCommands;
using AegisEInvoicing.Application.Features.UserManagement.Queries.GetAegisUserById;
using AegisEInvoicing.Application.Features.UserManagement.Queries.GetAegisUsers;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for Aegis user management operations
/// All operations enforce platform admin access only - Aegis users are not tied to any business
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/Aegis-user-management")]
public class AegisUserManagementController : BaseApiController
{
    /// <summary>
    /// Creates a new Aegis user (platform administrators only)
    /// Security: Only Aegis Platform Admins can create other Aegis users
    /// Note: Aegis users are NOT tied to any business and have platform-level access
    /// Password change is enforced on first login
    /// </summary>
    /// <param name="request">Aegis user creation request</param>
    /// <returns>Created Aegis user information</returns>
    [HttpPost("Aegis-users")]
    [RequireAegisAdmin]
    [SwaggerOperation(
        Summary = "Create Aegis platform user",
        Description = "Creates a new Aegis platform user with platform-level access. Password change is enforced on first login. Aegis admin privileges required.",
        OperationId = "CreateAegisUser",
        Tags = new[] { "Aegis User Management" }
    )]
    [ProducesResponseType(typeof(ApiResponse<CreateAegisUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAegisUser([FromBody] CreateAegisUserRequest request)
    {
        var command = new CreateAegisUserCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            //request.Password,
            request.AegisRole,
            request.PhoneNumber,
            request.AegisEmployeeId,
            request.AegisDepartment);

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            var statusCode = result.Message.Contains("Platform Admin") || result.Message.Contains("authorized")
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;
            return Error(result.Message, statusCode);
        }

        var response = new CreateAegisUserResponse(
            result.UserId!.Value,
            request.Email,
            request.AegisRole,
            "Password change required on first login");

        return Created($"/api/v1/user-management/Aegis-users/{result.UserId}",
            new ApiResponse<CreateAegisUserResponse>
            {
                Success = true,
                Data = response,
                Message = "Aegis user created successfully. Password change will be enforced on first login."
            });
    }

    /// <summary>
    /// Activates a Aegis user (platform administrators only)
    /// Security: Only Aegis platform admins can activate other Aegis users
    /// </summary>
    /// <param name="userId">Aegis User ID to activate</param>
    /// <returns>Activation result</returns>
    [HttpPost("users/{userId}/activate")]
    [RequireAegisAdmin]
    [SwaggerOperation(Summary = "Activate Aegis user", Description = "Activates a Aegis platform user account. Aegis admin privileges required.", OperationId = "ActivateAegisUser", Tags = new[] { "Aegis User Management" })]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ActivateAegisUser([FromRoute] Guid userId)
    {
        var command = new ActivateAegisUserCommand(userId);
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            var statusCode = result.Message.Contains("permissions") || result.Message.Contains("admin") || result.Message.Contains("Aegis")
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;
            return Error(result.Message, statusCode);
        }

        return Success<object?>(null, result.Message);
    }

    /// <summary>
    /// Deactivates a Aegis user (platform administrators only)
    /// Security: Only Aegis platform admins can deactivate other Aegis users
    /// </summary>
    /// <param name="userId">Aegis User ID to deactivate</param>
    /// <param name="request">Deactivation request with reason</param>
    /// <returns>Deactivation result</returns>
    [HttpPost("users/{userId}/deactivate")]
    [RequireAegisAdmin]
    [SwaggerOperation(Summary = "Deactivate Aegis user", Description = "Deactivates a Aegis platform user account with a reason. Aegis admin privileges required.", OperationId = "DeactivateAegisUser", Tags = new[] { "Aegis User Management" })]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivateAegisUser([FromRoute] Guid userId, [FromBody] DeactivateAegisUserRequest request)
    {
        var command = new DeactivateAegisUserCommand(userId, request.Reason);
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            var statusCode = result.Message.Contains("permissions") || result.Message.Contains("admin") || result.Message.Contains("Aegis")
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;
            return Error(result.Message, statusCode);
        }

        return Success<object?>(null, result.Message);
    }

    /// <summary>
    /// Updates a Aegis user's role (platform administrators only)
    /// Security: Only Aegis platform admins can update Aegis user roles
    /// </summary>
    /// <param name="userId">Aegis User ID to update</param>
    /// <param name="request">Role update request</param>
    /// <returns>Role update result</returns>
    [HttpPut("users/{userId}/role")]
    [RequireAegisAdmin]
    [SwaggerOperation(Summary = "Update Aegis user role", Description = "Updates the role of a Aegis platform user. Aegis admin privileges required.", OperationId = "UpdateAegisUserRole", Tags = new[] { "Aegis User Management" })]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateAegisUserRole([FromRoute] Guid userId, [FromBody] UpdateAegisUserRoleRequest request)
    {
        var command = new UpdateAegisUserRoleCommand(userId, request.NewAegisRole);
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            var statusCode = result.Message.Contains("permissions") || result.Message.Contains("admin") || result.Message.Contains("Aegis")
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;
            return Error(result.Message, statusCode);
        }

        return Success<object?>(null, result.Message);
    }

    /// <summary>
    /// Updates a Aegis user's profile (platform administrators only)
    /// Security: Only Aegis platform admins can update Aegis user profiles
    /// </summary>
    /// <param name="userId">Aegis User ID to update</param>
    /// <param name="request">Profile update request</param>
    /// <returns>Profile update result</returns>
    [HttpPut("users/{userId}/profile")]
    [RequireAegisAdmin]
    [SwaggerOperation(Summary = "Update Aegis user profile", Description = "Updates profile information for a Aegis platform user. Aegis admin privileges required.", OperationId = "UpdateAegisUserProfile", Tags = new[] { "Aegis User Management" })]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateAegisUserProfile([FromRoute] Guid userId, [FromBody] UpdateAegisUserProfileRequest request)
    {
        var command = new UpdateAegisUserProfileCommand(
            userId,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.AegisEmployeeId,
            request.AegisDepartment);

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            var statusCode = result.Message.Contains("permissions") || result.Message.Contains("admin") || result.Message.Contains("Aegis")
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;
            return Error(result.Message, statusCode);
        }

        return Success<object?>(null, result.Message);
    }

    /// <summary>
    /// Resets a Aegis user's password (platform administrators only)
    /// Security: Only Aegis platform admins can reset Aegis user passwords
    /// </summary>
    /// <param name="userId">Aegis User ID to reset password for</param>
    /// <param name="request">Password reset request</param>
    /// <returns>Password reset result</returns>
    [HttpPost("users/{userId}/reset-password")]
    [RequireAegisAdmin]
    [SwaggerOperation(Summary = "Reset Aegis user password", Description = "Resets the password for a Aegis platform user. Aegis admin privileges required.", OperationId = "ResetAegisUserPassword", Tags = new[] { "Aegis User Management" })]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetAegisUserPassword([FromRoute] Guid userId, [FromBody] ResetAegisUserPasswordRequest request)
    {
        var command = new ResetAegisUserPasswordCommand(userId, request.NewPassword);
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            var statusCode = result.Message.Contains("permissions") || result.Message.Contains("admin") || result.Message.Contains("Aegis")
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;
            return Error(result.Message, statusCode);
        }

        return Success<object?>(null, result.Message);
    }

    /// <summary>
    /// Deletes a Aegis user (platform administrators only)
    /// Security: Only Aegis platform admins can delete other Aegis users
    /// </summary>
    /// <param name="userId">Aegis User ID to delete</param>
    /// <param name="request">Deletion request with reason</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("users/{userId}")]
    [RequireAegisAdmin]
    [SwaggerOperation(Summary = "Delete Aegis user", Description = "Permanently deletes a Aegis platform user account with a reason. Aegis admin privileges required.", OperationId = "DeleteAegisUser", Tags = new[] { "Aegis User Management" })]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAegisUser([FromRoute] Guid userId, [FromBody] DeleteAegisUserRequest request)
    {
        var command = new DeleteAegisUserCommand(userId, request.Reason);
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            var statusCode = result.Message.Contains("permissions") || result.Message.Contains("admin") || result.Message.Contains("Aegis")
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;
            return Error(result.Message, statusCode);
        }

        return Success<object?>(null, result.Message);
    }
    /// <summary>
    /// Retrieves Aegis users with pagination and filtering (platform administrators only)
    /// Security: Only Aegis Platform Admins can view other Aegis users
    /// Supports filtering by role, status, department, and search terms
    /// </summary>
    /// <param name="searchTerm">Search term for name, email, employee ID, or department</param>
    /// <param name="status">Filter by user status</param>
    /// <param name="AegisRole">Filter by Aegis role</param>
    /// <param name="AegisDepartment">Filter by Aegis department</param>
    /// <param name="mustChangePassword">Filter by password change requirement</param>
    /// <param name="isEmailVerified">Filter by email verification status</param>
    /// <param name="createdAfter">Filter by creation date (after)</param>
    /// <param name="createdBefore">Filter by creation date (before)</param>
    /// <param name="lastLoginAfter">Filter by last login date (after)</param>
    /// <param name="lastLoginBefore">Filter by last login date (before)</param>
    /// <param name="sortBy">Sort field (FirstName, LastName, Email, CreatedAt, LastLoginAt, AegisRole)</param>
    /// <param name="sortDescending">Sort in descending order</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Paginated list of Aegis users</returns>
    [HttpGet("Aegis-users")]
    [RequireAegisAdmin]
    [SwaggerOperation(Summary = "Get all Aegis users", Description = "Retrieves paginated list of Aegis platform users with filtering and sorting. Aegis admin privileges required.", OperationId = "GetAegisUsers", Tags = new[] { "Aegis User Management" })]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAegisUsers(
        [FromQuery] string? searchTerm = null,
        [FromQuery] UserStatus? status = null,
        [FromQuery] AegisRole? AegisRole = null,
        [FromQuery] string? AegisDepartment = null,
        [FromQuery] bool? mustChangePassword = null,
        [FromQuery] bool? isEmailVerified = null,
        [FromQuery] DateTimeOffset? createdAfter = null,
        [FromQuery] DateTimeOffset? createdBefore = null,
        [FromQuery] DateTimeOffset? lastLoginAfter = null,
        [FromQuery] DateTimeOffset? lastLoginBefore = null,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        // Validate pagination parameters
        if (pageNumber < 1)
        {
            return Error("Page number must be greater than 0", StatusCodes.Status400BadRequest);
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Error("Page size must be between 1 and 100", StatusCodes.Status400BadRequest);
        }

        var query = new GetAegisUsersQuery(
            searchTerm,
            status,
            AegisRole,
            AegisDepartment,
            mustChangePassword,
            isEmailVerified,
            createdAfter,
            createdBefore,
            lastLoginAfter,
            lastLoginBefore,
            sortBy,
            sortDescending,
            pageNumber,
            pageSize);

        var result = await Mediator.Send(query);
        return Success(result, $"Retrieved {result.TotalCount} Aegis users");
    }

    /// <summary>
    /// Retrieves a single Aegis user by ID (platform administrators only)
    /// Security: Only Aegis Platform Admins can view Aegis user details
    /// Returns detailed information about the specified Aegis user
    /// </summary>
    /// <param name="userId">Aegis user ID to retrieve</param>
    /// <returns>Detailed Aegis user information</returns>
    [HttpGet("Aegis-users/{userId}")]
    [RequireAegisAdmin]
    [SwaggerOperation(Summary = "Get Aegis user by ID", Description = "Retrieves detailed information about a specific Aegis platform user. Aegis admin privileges required.", OperationId = "GetAegisUserById", Tags = new[] { "Aegis User Management" })]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAegisUserById([FromRoute] Guid userId)
    {
        var query = new GetAegisUserByIdQuery(userId);
        var result = await Mediator.Send(query);

        if (result == null)
        {
            return Error("Aegis user not found", StatusCodes.Status404NotFound);
        }

        return Success(result, "Aegis user retrieved successfully");
    }
}

// Request/Response models for Aegis User Management
public record CreateAegisUserRequest(
    string FirstName,
    string LastName,
    string Email,
    //string Password,
    AegisRole AegisRole,
    string? PhoneNumber,
    string? AegisEmployeeId,
    string? AegisDepartment);

public record DeactivateAegisUserRequest(string Reason);

public record DeleteAegisUserRequest(string Reason);

public record UpdateAegisUserRoleRequest(AegisRole NewAegisRole);

public record UpdateAegisUserProfileRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? AegisEmployeeId,
    string? AegisDepartment);

public record ResetAegisUserPasswordRequest(string NewPassword);


public record CreateAegisUserResponse(
    Guid UserId,
    string Email,
    AegisRole AegisRole,
    string PasswordChangeNote);