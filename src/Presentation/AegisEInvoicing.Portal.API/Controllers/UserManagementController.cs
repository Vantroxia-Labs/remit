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
using Swashbuckle.AspNetCore.Annotations;

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
  [RequireClientAdmin]
  [SwaggerOperation(Summary = "List Business Users", Description = "Returns all users in the authenticated admin's business.")]
  [ProducesResponseType(typeof(ApiResponse<PaginatedList<UserDto>>), StatusCodes.Status200OK)]
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
  [RequireClientAdmin]
  [SwaggerOperation(
      Summary = "Create Business User",
      Description = @"Creates a new user account within the authenticated administrator's business.

**Features:**
- Auto-generated temporary password sent via email
- Email verification required (if enabled)
- Support for multiple role assignments
- User audit trail creation

**Tenant Isolation:**
- Users can only be created within the authenticated admin's business
- Cross-tenant user creation is strictly prohibited
- Business ID is automatically assigned from authenticated user context

**Access Control:**
- **Required Role**: Business Administrator
- **Permission**: CreateUsers

**Validation Rules:**
- Email must be unique across the entire platform
- Phone number must be valid format
- At least one role must be assigned
- First and last names are required
- Email format must be valid

**User Initial State:**
- **Status**: Active (if email verification disabled) or Pending
- **MustChangePassword**: true (user must change password on first login)
- **EmailVerified**: false (requires email verification)

**Example Request:**
```json
{
  ""firstName"": ""John"",
  ""lastName"": ""Doe"",
  ""email"": ""john.doe@example.com"",
  ""phoneNumber"": ""+2348012345678"",
  ""roleIds"": [
    ""3fa85f64-5717-4562-b3fc-2c963f66afa6""
  ]
}
```

**Example Response:**
```json
{
  ""data"": {
    ""userId"": ""9d7e6f5c-4321-8765-bcde-f012345678ab"",
    ""email"": ""john.doe@example.com""
  },
  ""message"": ""User created successfully. Temporary password sent to email."",
  ""isSuccess"": true,
  ""statusCode"": 201
}
```"
  )]
  [ProducesResponseType(typeof(ApiResponse<CreateUserResponse>), StatusCodes.Status201Created)]
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
  //[RequirePermission(PermissionConstants.ViewUsers)]
  [SwaggerOperation(
      Summary = "Get Business Users List",
      Description = @"Retrieves a paginated list of all users belonging to a specific business.

**Features:**
- Pagination support for large user lists
- Returns user details including roles and status
- Tenant-isolated user listing
- Includes user activity information

**Access Control:**
- **Required Role**: Business Administrator
- **Tenant Isolation**: Admins can only view users from their own business
- Cross-tenant user access is strictly prohibited

**Returned User Information:**
- User ID and personal details (name, email, phone)
- Assigned roles and permissions
- Account status (Active, Inactive, Locked)
- Email verification status
- Last login timestamp
- Creation and modification dates

**Pagination:**
- `pageIndex`: Page number (1-based indexing)
- `pageSize`: Number of users per page (recommended: 10-50)
- Response includes total count and page information

**Example Response:**
```json
{
  ""data"": {
    ""items"": [
      {
        ""id"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
        ""firstName"": ""John"",
        ""lastName"": ""Doe"",
        ""email"": ""john.doe@example.com"",
        ""phoneNumber"": ""+2348012345678"",
        ""isActive"": true,
        ""emailVerified"": true,
        ""roles"": [
          {
            ""roleId"": ""7c8e9f10-1234-5678-9abc-def012345678"",
            ""roleName"": ""Business User""
          }
        ],
        ""lastLoginAt"": ""2025-01-14T15:30:00Z"",
        ""createdAt"": ""2025-01-01T10:00:00Z""
      }
    ],
    ""pageNumber"": 1,
    ""totalPages"": 5,
    ""totalCount"": 48,
    ""hasPreviousPage"": false,
    ""hasNextPage"": true
  },
  ""message"": ""Request successful"",
  ""isSuccess"": true,
  ""statusCode"": 200
}
```"
  )]
  [ProducesResponseType(typeof(ApiResponse<PaginatedList<UserDto>>), StatusCodes.Status200OK)]
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
  //[RequirePermission(PermissionConstants.ViewUsers)]
  [SwaggerOperation(
      Summary = "Get Business User by ID",
      Description = @"Retrieves detailed information about a specific user within a business.

**Features:**
- Complete user profile information
- All assigned roles and permissions
- Account status and activity history
- Email and phone verification status
- Audit trail information

**Access Control:**
- **Required Role**: Business Administrator
- **Tenant Isolation**: Admins can only view users from their own business
- Cross-tenant user access is strictly prohibited

**Returned Information:**
- User ID and personal details (first name, last name, email, phone)
- All assigned roles with expiration dates (if applicable)
- Account status (Active, Inactive, Locked, Pending)
- Email verification status
- Phone verification status
- Password change requirements
- Last login timestamp
- Failed login attempts
- Creation and last modification timestamps
- Created by and modified by information

**Example Response:**
```json
{
  ""data"": {
    ""id"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
    ""firstName"": ""John"",
    ""lastName"": ""Doe"",
    ""email"": ""john.doe@example.com"",
    ""phoneNumber"": ""+2348012345678"",
    ""isActive"": true,
    ""emailVerified"": true,
    ""phoneVerified"": false,
    ""mustChangePassword"": false,
    ""roles"": [
      {
        ""roleId"": ""7c8e9f10-1234-5678-9abc-def012345678"",
        ""roleName"": ""Business User"",
        ""assignedAt"": ""2025-01-01T10:00:00Z"",
        ""expiresAt"": null
      }
    ],
    ""lastLoginAt"": ""2025-01-14T15:30:00Z"",
    ""failedLoginAttempts"": 0,
    ""createdAt"": ""2025-01-01T10:00:00Z"",
    ""modifiedAt"": ""2025-01-10T14:20:00Z""
  },
  ""message"": ""Request successful"",
  ""isSuccess"": true,
  ""statusCode"": 200
}
```"
  )]
  [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
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
  //[RequirePermission(PermissionConstants.ActivateUsers)]
  [SwaggerOperation(
      Summary = "Activate User",
      Description = @"Activates a previously deactivated or suspended user account within the business.

**Features:**
- Restores user access to the system
- Clears account lockout status
- Resets failed login attempt counter
- Preserves all user data and role assignments
- Creates audit trail entry for activation

**Access Control:**
- **Required Role**: Business Administrator
- **Tenant Isolation**: Admins can only activate users from their own business
- Cross-tenant user activation is strictly prohibited

**Use Cases:**
- Reactivate temporarily suspended user accounts
- Restore access after account lockout
- Re-enable user after administrative review
- Restore accidentally deactivated accounts

**Effects of Activation:**
- User status changes to Active
- User can log in immediately
- All assigned roles and permissions are restored
- Failed login attempts counter is reset
- Account lockout is cleared

**Example Response:**
```json
{
  ""data"": null,
  ""message"": ""User activated successfully"",
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
  //[RequirePermission(PermissionConstants.DeactivateUsers)]
  [SwaggerOperation(
      Summary = "Deactivate User",
      Description = @"Deactivates a user account within the business, preventing login while preserving all data.

**Features:**
- Soft deactivation - user data is retained
- Prevents user from logging in
- Revokes all active sessions immediately
- Invalidates all existing tokens
- Preserves role assignments for potential reactivation
- Requires administrative reason for audit trail

**Access Control:**
- **Required Role**: Business Administrator
- **Tenant Isolation**: Admins can only deactivate users from their own business
- Cross-tenant user deactivation is strictly prohibited

**Use Cases:**
- Temporarily suspend user access
- Disable accounts during investigations
- Suspend users pending review
- Enforce administrative actions

**Effects of Deactivation:**
- User status changes to Inactive
- User cannot log in
- All active sessions are terminated
- All tokens (access and refresh) are revoked
- Role assignments are preserved
- User data remains in the database

**Validation Rules:**
- Reason is required and must be descriptive
- Cannot deactivate the last active admin
- Cannot deactivate Aegis system administrators

**Example Request:**
```json
{
  ""reason"": ""User account suspended pending security review""
}
```

**Example Response:**
```json
{
  ""data"": null,
  ""message"": ""User deactivated successfully"",
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
  //[RequirePermission(PermissionConstants.DeleteUsers)]
  [SwaggerOperation(
      Summary = "Delete User",
      Description = @"Soft deletes a user account within the business, marking it as deleted while retaining data for audit purposes.

**Features:**
- Soft delete - data retained for audit and compliance
- Revokes all active sessions immediately
- Invalidates all existing tokens
- Removes user from all role assignments
- Prevents future login attempts
- Requires administrative reason for audit trail

**Access Control:**
- **Required Role**: Business Administrator
- **Tenant Isolation**: Admins can only delete users from their own business
- Cross-tenant user deletion is strictly prohibited

**Use Cases:**
- Remove user accounts permanently
- Comply with user removal requests
- Delete compromised accounts
- Remove terminated employees

**Effects of Deletion:**
- User status changes to Deleted
- User cannot log in
- All active sessions are terminated immediately
- All tokens (access and refresh) are revoked
- All role assignments are removed
- User data remains in database (soft delete for audit)
- Email address is released for potential reuse

**Validation Rules:**
- Reason is required and must be descriptive
- Cannot delete the last active admin
- Cannot delete Aegis system administrators
- Cannot delete users with active invoices (depending on business rules)

**Note:** This is a soft delete operation. The user record remains in the database for audit purposes but is marked as deleted and cannot be restored through normal reactivation.

**Example Request:**
```json
{
  ""reason"": ""User requested account deletion per GDPR compliance""
}
```

**Example Response:**
```json
{
  ""data"": null,
  ""message"": ""User deleted successfully"",
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
  //[RequirePermission(PermissionConstants.ResetPasswords)]
  [SwaggerOperation(
      Summary = "Reset User Password",
      Description = @"Administratively resets a user's password within the business.

**Features:**
- Admin-initiated password reset
- Sets new password provided by administrator
- Forces user to change password on next login
- Revokes all existing sessions and tokens
- Sends password change notification email
- Creates audit trail entry

**Access Control:**
- **Required Role**: Business Administrator
- **Tenant Isolation**: Admins can only reset passwords for users in their own business
- Cross-tenant password reset is strictly prohibited

**Use Cases:**
- Reset password for users who forgot their password
- Emergency access recovery
- Security incident response
- New employee onboarding with temporary password

**Password Requirements:**
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character
- Cannot be a commonly used password

**Effects of Password Reset:**
- User's password is changed to the provided value
- `MustChangePassword` flag is set to true
- All active sessions are terminated
- All tokens (access and refresh) are revoked
- User receives email notification
- Password change is logged in audit trail

**Security Considerations:**
- Use strong temporary passwords
- Communicate password securely (avoid email)
- Ensure user changes password on first login
- Monitor for suspicious password reset patterns

**Example Request:**
```json
{
  ""newPassword"": ""TempP@ssw0rd123""
}
```

**Example Response:**
```json
{
  ""data"": null,
  ""message"": ""Password reset successfully. User must change password on next login."",
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
  //[RequirePermission(PermissionConstants.AssignRoles)]
  [SwaggerOperation(
      Summary = "Assign Role to User",
      Description = @"Assigns a role to a user within the business, granting associated permissions.

**Features:**
- Add new roles to existing users
- Support for multiple concurrent roles
- Optional role expiration dates
- Automatic permission grant
- Duplicate role prevention
- Audit trail creation

**Access Control:**
- **Required Role**: Business Administrator
- **Tenant Isolation**: Admins can only assign roles to users in their own business
- Cross-tenant role assignment is strictly prohibited

**Available Roles:**
- **Business Administrator**: Full administrative access to business
- **Business User**: Standard user access with invoice management
- **Finance Manager**: Financial operations and reporting
- **Auditor**: Read-only access for audit purposes

**Use Cases:**
- Grant additional permissions to users
- Promote users to administrative roles
- Assign temporary roles with expiration
- Role-based access control management

**Role Assignment Rules:**
- User must belong to the same business as admin
- Role must exist and be active
- User can have multiple roles simultaneously
- Duplicate role assignments are prevented
- Expired roles are automatically revoked

**Expiration:**
- Optional `expiresAt` parameter for temporary roles
- Null value = permanent role assignment
- Expired roles are automatically revoked
- User receives notification before role expiration

**Example Request (Permanent Role):**
```json
{
  ""roleId"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
  ""expiresAt"": null
}
```

**Example Request (Temporary Role):**
```json
{
  ""roleId"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
  ""expiresAt"": ""2025-12-31T23:59:59Z""
}
```

**Example Response:**
```json
{
  ""data"": null,
  ""message"": ""Role assigned successfully"",
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
  //[RequirePermission(PermissionConstants.RevokeRoles)]
  [SwaggerOperation(
      Summary = "Revoke Role from User",
      Description = @"Revokes a role from a user within the business, removing associated permissions.

**Features:**
- Remove roles from existing users
- Immediate permission revocation
- Optional reason for audit trail
- User notification
- Session and token validation
- Audit trail creation

**Access Control:**
- **Required Role**: Business Administrator
- **Tenant Isolation**: Admins can only revoke roles from users in their own business
- Cross-tenant role revocation is strictly prohibited

**Use Cases:**
- Remove excess permissions from users
- Demote users from administrative roles
- Revoke temporary roles before expiration
- Security incident response
- Organizational role changes

**Role Revocation Rules:**
- User must belong to the same business as admin
- Role must currently be assigned to the user
- Cannot revoke the last admin role if user is the last admin
- User's active sessions may require re-authentication
- Reason is optional but recommended for audit purposes

**Effects of Revocation:**
- Role is immediately removed from user
- Associated permissions are revoked
- User may lose access to certain features
- Active sessions remain valid but permissions are updated
- User is notified of role change
- Revocation is logged in audit trail

**Validation:**
- Cannot revoke role that user doesn't have
- Cannot leave business without at least one admin
- Cannot revoke system-required roles

**Example Request (with reason):**
```json
{
  ""reason"": ""User role changed due to department transfer""
}
```

**Example Request (without reason):**
```json
{}
```

**Example Response:**
```json
{
  ""data"": null,
  ""message"": ""Role revoked successfully"",
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


