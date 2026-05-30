using Asp.Versioning;
using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using AegisEInvoicing.Application.Features.RoleManagement.Queries.GetBusinessRoles;
using AegisEInvoicing.Application.Features.RoleManagement.Commands.CreateBusinessRole;
using AegisEInvoicing.Application.Features.RoleManagement.Commands.UpdateBusinessRolePermissions;
using AegisEInvoicing.Application.Features.RoleManagement.Commands.DeleteBusinessRole;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Manages custom roles created by ClientAdmins for their business.
/// System roles (seeded by Aegis) are read-only through this controller.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/role-management")]
public class RoleManagementController : BaseApiController
{
    /// <summary>
    /// Returns all roles visible to the caller: system roles + custom roles owned by their business.
    /// </summary>
    [HttpGet("roles")]
    [RequireClientAdmin]
    [ProducesResponseType(typeof(ApiResponse<List<PlatformRoleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetBusinessRolesQuery(), cancellationToken);
        return Success(result, "Roles retrieved successfully.");
    }

    /// <summary>
    /// Creates a new custom role for the caller's business.
    /// Only permissions from PermissionConstants.ClientAdminAssignablePermissions are allowed.
    /// </summary>
    [HttpPost("roles")]
    [RequireClientAdmin]
    [ProducesResponseType(typeof(ApiResponse<CreateBusinessRoleResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateBusinessRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateBusinessRoleCommand(
            request.Name,
            request.Description,
            request.Permissions);

        var result = await Mediator.Send(command, cancellationToken);
        return Created($"/api/v1/role-management/roles/{result.RoleId}", result);
    }

    /// <summary>
    /// Updates the permission set of an existing custom business role.
    /// System roles cannot be modified through this endpoint.
    /// </summary>
    [HttpPut("roles/{roleId:guid}/permissions")]
    [RequireClientAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRolePermissions(
        [FromRoute] Guid roleId,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateBusinessRolePermissionsCommand(roleId, request.Permissions);
        await Mediator.Send(command, cancellationToken);
        return Success<object?>(null, "Permissions updated successfully.");
    }

    /// <summary>
    /// Soft-deletes a custom business role.
    /// Fails if the role has active user assignments.
    /// </summary>
    [HttpDelete("roles/{roleId:guid}")]
    [RequireClientAdmin]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(
        [FromRoute] Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteBusinessRoleCommand(roleId);
        var result = await Mediator.Send(command, cancellationToken);
        return Success<object?>(null, result.Message);
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public record CreateBusinessRoleRequest(
    string Name,
    string Description,
    IReadOnlyList<string> Permissions);

public record UpdateRolePermissionsRequest(IReadOnlyList<string> Permissions);
