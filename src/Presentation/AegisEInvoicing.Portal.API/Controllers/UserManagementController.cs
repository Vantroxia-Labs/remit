using Asp.Versioning;
using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;
using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using AegisEInvoicing.Application.Features.UserManagement.Queries.GetBusinessUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for user management operations within tenants
/// All operations enforce tenant isolation and admin-only access
/// </summary>
[ApiVersion("1.0")]
[Authorize] // All user management operations require authentication
[Route("api/v{version:apiVersion}/user-management")]
[Route("api/v{version:apiVersion}/usermanagement")]
public class UserManagementController(ICurrentUserService currentUserService) : BaseApiController
{
  private readonly ICurrentUserService _currentUserService = currentUserService;

  /// <summary>
  /// Lists users belonging to the currently authenticated admin's business.
  /// </summary>
  [HttpGet("users")]
  [RequireClientAdmin]  [ProducesResponseType(typeof(ApiResponse<PaginatedList<UserDto>>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
  public async Task<IActionResult> GetUsers(
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 50,
      CancellationToken cancellationToken = default)
  {
    if (!_currentUserService.BusinessId.HasValue)
      return Error("User is not associated with a business", StatusCodes.Status403Forbidden);

    var query = new GetBusinessUsersQuery(_currentUserService.BusinessId.Value.ToString(), page, pageSize);
    var result = await Mediator.Send(query, cancellationToken);
    return Success(result, "Users retrieved successfully");
  }
  /// <summary>
  /// Creates a new user within the authenticated admin's tenant
  /// </summary>
  /// <param name="request">User creation request with name, email, phone, and roles</param>
  /// <returns>Created user information with generated user ID</returns>
  [HttpPost("users")]
  [RequireClientAdmin]  [ProducesResponseType(typeof(ApiResponse<CreateUserResponse>), StatusCodes.Status201Created)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
  {
    var command = new CreateUserCommand(
        request.FirstName,
        request.LastName,
        request.Email,
        //request.Password,
        request.PhoneNumber,
        request.RoleIds);

    var result = await Mediator.Send(command);

    if (!result.IsSuccess)
    {
      var statusCode = result.Message.Contains("permissions") || result.Message.Contains("admin")
          ? StatusCodes.Status403Forbidden
          : StatusCodes.Status400BadRequest;
      return Error(result.Message, statusCode);
    }

    var response = new CreateUserResponse(result.UserId!.Value, request.Email);
    return Created($"/api/v1/user-management/users/{result.UserId}", response);
  }

  /// <summary>
  /// Fetch a list of business users
  /// Security: Only tenant admins can fetch users in their own tenant
  /// </summary>
  /// <param name="businessId">Business ID to fetch users from</param>
  /// <param name="pageIndex">Page index for pagination (1-based)</param>
  /// <param name="pageSize">Number of users per page</param>
  /// <returns>Paginated list of users in the business</returns>
  [HttpGet("fetch-users/{businessId}/{pageIndex}/{pageSize}")]
  [RequireClientAdmin]
  //[RequirePermission(PermissionConstants.ViewUsers)]  [ProducesResponseType(typeof(ApiResponse<PaginatedList<UserDto>>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<PaginatedList<UserDto>>), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse<PaginatedList<UserDto>>), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> FetchUsers(string businessId, int pageIndex, int pageSize)
  {
    // Validate businessId format to prevent injection attacks (VAPT finding: time-based SQL injection)
    if (!Guid.TryParse(businessId, out _))
    {
      return BadRequest(Error("Invalid business ID format"));
    }

    var query = new GetBusinessUsersQuery(businessId, pageIndex, pageSize);
    var result = await Mediator.Send(query);

    return Success(result, "Request successful");
  }

  /// <summary>
  /// Fetch a single business user
  /// Security: Only tenant admins can fetch users in their own tenant
  /// </summary>
  /// <param name="businessId">Business ID the user belongs to</param>
  /// <param name="userId">User ID to retrieve</param>
  /// <returns>Detailed user information</returns>
  [HttpGet("fetch-user/{businessId}/{userId}")]
  [RequireClientAdmin]
  //[RequirePermission(PermissionConstants.ViewUsers)]  [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> FetchUser(string businessId, string userId)
  {
    // Validate ID formats to prevent injection attacks (VAPT finding: time-based SQL injection)
    if (!Guid.TryParse(businessId, out _) || !Guid.TryParse(userId, out _))
    {
      return BadRequest(Error("Invalid business ID or user ID format"));
    }

    var query = new GetBusinessUserByIdQuery(businessId, userId);
    var result = await Mediator.Send(query);

    return Success(result, "Request successful");
  }

  /// <summary>
  /// Activates a user within the authenticated admin's tenant
  /// Security: Only tenant admins can activate users in their own tenant
  /// </summary>
  /// <param name="userId">User ID to activate</param>
  /// <returns>Activation result</returns>
  [HttpPost("users/{userId}/activate")]
  [RequireClientAdmin]
  //[RequirePermission(PermissionConstants.ActivateUsers)]  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> ActivateUser([FromRoute] Guid userId)
  {
    var command = new ActivateUserCommand(userId);
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

  /// <summary>
  /// Deactivates a user within the authenticated admin's tenant
  /// Security: Only tenant admins can deactivate users in their own tenant
  /// </summary>
  /// <param name="userId">User ID to deactivate</param>
  /// <param name="request">Deactivation request with reason</param>
  /// <returns>Deactivation result</returns>
  [HttpPost("users/{userId}/deactivate")]
  [RequireClientAdmin]
  //[RequirePermission(PermissionConstants.DeactivateUsers)]  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> DeactivateUser([FromRoute] Guid userId, [FromBody] DeactivateUserRequest request)
  {
    var command = new DeactivateUserCommand(userId, request.Reason);
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

  /// <summary>
  /// Deletes a user within the authenticated admin's tenant
  /// Security: Only tenant admins can delete users in their own tenant
  /// </summary>
  /// <param name="userId">User ID to delete</param>
  /// <param name="request">Deletion request with reason</param>
  /// <returns>Deletion result</returns>
  [HttpDelete("users/{userId}")]
  [RequireClientAdmin]
  //[RequirePermission(PermissionConstants.DeleteUsers)]  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> DeleteUser([FromRoute] Guid userId, [FromBody] DeleteUserRequest request)
  {
    var command = new DeleteUserCommand(userId, request.Reason);
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

  /// <summary>
  /// Resets a user's password within the authenticated admin's tenant
  /// Security: Only tenant admins can reset passwords for users in their own tenant
  /// </summary>
  /// <param name="userId">User ID to reset password for</param>
  /// <param name="request">Password reset request</param>
  /// <returns>Password reset result</returns>
  [HttpPost("users/{userId}/reset-password")]
  [RequireClientAdmin]
  //[RequirePermission(PermissionConstants.ResetPasswords)]  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> ResetPassword([FromRoute] Guid userId, [FromBody] ResetPasswordRequest request)
  {
    var command = new ResetPasswordCommand(userId, request.NewPassword);
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

  /// <summary>
  /// Assigns a role to a user within the authenticated admin's tenant
  /// Security: Only tenant admins can assign roles to users in their own tenant
  /// </summary>
  /// <param name="userId">User ID to assign role to</param>
  /// <param name="request">Role assignment request</param>
  /// <returns>Role assignment result</returns>
  [HttpPost("users/{userId}/roles")]
  [RequireClientAdmin]
  //[RequirePermission(PermissionConstants.AssignRoles)]  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> AssignRole([FromRoute] Guid userId, [FromBody] AssignRoleRequest request)
  {
    var command = new AssignRoleCommand(userId, request.RoleId, request.ExpiresAt);
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

  /// <summary>
  /// Revokes a role from a user within the authenticated admin's tenant
  /// Security: Only tenant admins can revoke roles from users in their own tenant
  /// </summary>
  /// <param name="userId">User ID to revoke role from</param>
  /// <param name="roleId">Role ID to revoke</param>
  /// <param name="request">Role revocation request</param>
  /// <returns>Role revocation result</returns>
  [HttpDelete("users/{userId}/roles/{roleId}")]
  [RequireClientAdmin]
  //[RequirePermission(PermissionConstants.RevokeRoles)]  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> RevokeRole([FromRoute] Guid userId, [FromRoute] Guid roleId, [FromBody] RevokeRoleRequest? request = null)
  {
    var command = new RevokeRoleCommand(userId, roleId, request?.Reason);
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

// Request/Response models
public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    //string Password,
    string? PhoneNumber,
    IEnumerable<Guid> RoleIds);

public record CreateUserResponse(Guid UserId, string Email);

public record DeactivateUserRequest(string Reason);

public record DeleteUserRequest(string Reason);

public record ResetPasswordRequest(string NewPassword);

public record AssignRoleRequest(Guid RoleId, DateTimeOffset? ExpiresAt);

public record RevokeRoleRequest(string? Reason);


