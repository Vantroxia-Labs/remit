using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.Business.Request;
using AegisEInvoicing.Portal.API.Models.Business.Response;
using AegisEInvoicing.Portal.API.Models.BusinessOnboarding.Response;
using AegisEInvoicing.Portal.API.Models.SftpUser;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.ActivateBusiness;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.SuspendBusiness;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.UpdateBusiness;
using AegisEInvoicing.Application.Features.Miscellenous.DTOs;
using AegisEInvoicing.Application.Features.Miscellenous.Queries;
using AegisEInvoicing.Application.Features.PlatformSubscriptions.Queries.GetAllPlatformSubscriptions;
using AegisEInvoicing.Application.Features.SftpUserManagement.Commands.AddVirtualDirectoryToUser;
using AegisEInvoicing.Application.Features.SftpUserManagement.Commands.ChangeSftpPassword;
using AegisEInvoicing.Application.Features.SftpUserManagement.Commands.DeleteSftpUser;
using AegisEInvoicing.Application.Features.SftpUserManagement.Commands.EnsureSFTPGoUserFromDb;
using AegisEInvoicing.Application.Features.SftpUserManagement.Commands.EnsureUserDirectories;
using AegisEInvoicing.Application.Features.SftpUserManagement.Commands.RenameSftpUser;
using AegisEInvoicing.Application.Features.SftpUserManagement.Queries.GetAllSftpUsers;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for Aegis Platform Admin only
/// KMPG onboards businesses for project usage and manages all FIRS interactions for SaaS/API solutions
/// All business onboarding and subscription activation is managed exclusively by KMPG platform administrators
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[SwaggerTag("Business Operations which includes onboarding, updating, fetching business")]
[Authorize]
[RequireRole(RoleConstants.AegisAdmin)]
public class AegisAdminController(
    IMediator mediator,
    ILogger<AegisAdminController> logger
   ) : BaseApiController
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<AegisAdminController> _logger = logger;
   


    /// <summary>
    /// Get list of all available platform subscriptions (Aegis Platform Admin only)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all available platform subscriptions</returns>
    [HttpGet("platform-subscriptions")]
    [SwaggerOperation(
        Summary = "Get All Platform Subscriptions (Aegis Admin Only)",
        Description = "Retrieves all available platform subscriptions. Only KMPG platform administrators can access subscription information."
    )]
    [SwaggerResponse(200, "Platform subscriptions retrieved successfully", typeof(ApiResponse<IEnumerable<PlatformSubscriptionResponse>>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GetPlatformSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("KMPG platform admin requested all platform subscriptions list");

        var query = new GetAllPlatformSubscriptionsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        var response = result.Select(dto => new PlatformSubscriptionResponse
        {
            Id = dto.Id,
            PlanName = dto.PlanName,
            Tier = dto.Tier,
            MonthlyPrice = dto.MonthlyPrice,
            Currency = dto.Currency
        }).ToList();

        _logger.LogInformation("Successfully retrieved {Count} platform subscriptions", response.Count);

        return Success(result, string.Empty);
    }

    [HttpGet("all-platform-roles")]
    [SwaggerOperation(Description = "This endpoint allows fetching list of platform system roles")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<PlatformBusinessRoleSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<PlatformBusinessRoleSummaryDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<PlatformBusinessRoleSummaryDto>>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPlatformRoles(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching list of platform roles");

        var request = new PlatformBusinessRolesQuery(isBusiness: false);

        var result = await _mediator.Send(request, cancellationToken);

        return Success(result, "List of platform roles retrieved successfully");
    }


    [HttpGet("all-user-roles")]
    [SwaggerOperation(Description = "This endpoint allows fetching list of user roles")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<UserRolesSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<UserRolesSummaryDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<UserRolesSummaryDto>>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserRoles(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching list of user roles");

        var request = new UserRolesQuery();

        var result = await _mediator.Send(request, cancellationToken);

        return Success(result, "List of user roles retrieved successfully");
    }


    [HttpPost("activate-business")]
    [SwaggerOperation(Description = "This endpoint allows Aegis admin user to activate a business.")]
    [SwaggerResponse(200, "Business activated successfully", typeof(ApiResponse<ReactivateBusinessResult>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<ReactivateBusinessResult>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<ReactivateBusinessResult>))]
    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<ReactivateBusinessResult>))]
    [SwaggerResponse(500, "Access denied - insufficient permissions", typeof(ApiResponse<ReactivateBusinessResult>))]    
    public async Task<IActionResult> ActivateBusiness([FromBody] ReactivateBusinessCommand command)
    {
        _logger.LogInformation("KMPG platform admin about activating a business");

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to activate a business: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }

    [HttpPost("deactivate-business")]
    [SwaggerOperation(Description = "This endpoint allows Aegis admin user to deactivate a business.")]
    [SwaggerResponse(200, "Business deactivated successfully", typeof(ApiResponse<SuspendBusinessResult>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<SuspendBusinessResult>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<SuspendBusinessResult>))]
    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<SuspendBusinessResult>))]
    [SwaggerResponse(500, "Access denied - insufficient permissions", typeof(ApiResponse<SuspendBusinessResult>))]
    public async Task<IActionResult> DeactivateBusiness([FromBody] SuspendBusinessCommand command)
    {
        _logger.LogInformation("KMPG platform admin about deactivating a business");

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to deactivate a business: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }

    [HttpPatch("update-business/{businessId}")]
    [RequireRole(RoleConstants.AegisAdmin)]
    [SwaggerOperation(
        Summary = "Update Business by (Aegis Admin Only)",
        Description = "Update business to EInvoice Integrator platform. Only KMPG platform administrators can perform business update. KMPG manages all FIRS interactions for SaaS/API solutions and activates business subscriptions.")]
    [SwaggerResponse(200, "Update successful", typeof(ApiResponse<OnboardBusinessResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<OnboardBusinessResponse>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<OnboardBusinessResponse>))]
    public async Task<IActionResult> UpdateBusiness([FromBody] UpdateBusinessRequest request, Guid businessId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("KMPG business update to EInvoice Integrator platform");

        var address = Address.Create(
            request.RegisteredAddress.Street,
            request.RegisteredAddress.City,
            request.RegisteredAddress.State,
            request.RegisteredAddress.Country,
            request.RegisteredAddress.PostalCode);

        var command = new UpdateBusinessCommand(businessId, address, request.InvoicePrefix, request.Industry, request.ContactEmail, request.ContactPhone, request.Description);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to update business to platform: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        _logger.LogInformation("KMPG successfully update business to EInvoice Integrator platform with {Message}", result.Message);

        var response = new UpdateBusinessResponse
        {
            Status = result.IsSuccess!,
            Message = result.Message
        };

        return Success(response, "Business successfully updated to EInvoice Integrator platform");
    }
    
    // SFTP User Management Endpoints
    
    /// <summary>
    /// Get all SFTP users from database (Aegis Admin Only)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all SFTP users</returns>
    [HttpGet("sftp-users")]
    [SwaggerOperation(
        Summary = "Get All SFTP Users (Aegis Admin Only)",
        Description = "Retrieves all SFTP users from the database. Only KMPG administrators can access this information."
    )]
    [SwaggerResponse(200, "SFTP users retrieved successfully", typeof(ApiResponse<IEnumerable<Application.Features.SftpUserManagement.Queries.GetAllSftpUsers.SftpUserDto>>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GetSftpUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("KMPG platform admin requested all SFTP users list");

        var query = new GetAllSftpUsersQuery();
        var sftpUsers = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation("Successfully retrieved {Count} SFTP users", sftpUsers.Count());
        return Success(sftpUsers, "SFTP users retrieved successfully");
    }


    /// <summary>
    /// Change SFTP user password in database (Aegis Admin Only)
    /// </summary>
    /// <param name="request">Request containing username and new password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    [HttpPost("sftp-users/change-password")]
    [SwaggerOperation(
        Summary = "Change SFTP User Password (Aegis Admin Only)",
        Description = "Changes SFTP user password in database. Only KMPG administrators can perform this operation."
    )]
    [SwaggerResponse(200, "Password changed successfully", typeof(ApiResponse<SftpOperationResponse>))]
    [SwaggerResponse(400, "Invalid request or password change failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<IActionResult> ChangeSftpPasswordAsync([FromBody] ChangeSftpPasswordRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Changing password for SFTP user: {Username}", request.Username);

        var command = new ChangeSftpPasswordCommand(request.Username, "", request.NewPassword);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to change password for SFTP user {Username}: {Message}", 
                request.Username, result.Message);
            return BadRequest(Error(result.Message));
        }

        _logger.LogInformation("Successfully changed password for SFTP user: {Username}", request.Username);
        return Success(new SftpOperationResponse { Success = true, Message = result.Message }, 
            result.Message);
    }

    /// <summary>
    /// Rename SFTP user in SFTPGo and database (Aegis Admin Only)
    /// </summary>
    /// <param name="request">Request containing current and new username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    [HttpPost("sftp-users/rename")]
    [SwaggerOperation(
        Summary = "Rename SFTP User (Aegis Admin Only)",
        Description = "Renames SFTP user in both SFTPGoService and database. Only KMPG administrators can perform this operation."
    )]
    [SwaggerResponse(200, "User renamed successfully", typeof(ApiResponse<SftpOperationResponse>))]
    [SwaggerResponse(400, "Invalid request or rename failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<IActionResult> RenameSftpUserAsync([FromBody] RenameSftpUserRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Renaming SFTP user from {Username} to {NewUsername}", request.Username, request.NewUsername);

        var command = new RenameSftpUserCommand(request.Username, request.NewUsername);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to rename SFTP user from {Username} to {NewUsername}: {Message}", 
                request.Username, request.NewUsername, result.Message);
            return BadRequest(Error(result.Message));
        }

        _logger.LogInformation("Successfully renamed SFTP user from {Username} to {NewUsername}", request.Username, request.NewUsername);
        return Success(new SftpOperationResponse { Success = true, Message = result.Message }, 
            result.Message);
    }

    /// <summary>
    /// Delete SFTP user from SFTPGo and database (Aegis Admin Only)
    /// </summary>
    /// <param name="username">Username to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    [HttpDelete("sftp-users/{username}")]
    [SwaggerOperation(
        Summary = "Delete SFTP User (Aegis Admin Only)",
        Description = "Deletes SFTP user from both SFTPGoService and database. Only KMPG administrators can perform this operation."
    )]
    [SwaggerResponse(200, "User deleted successfully", typeof(ApiResponse<SftpOperationResponse>))]
    [SwaggerResponse(400, "Invalid request or deletion failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<IActionResult> DeleteSftpUserAsync(string username, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting SFTP user: {Username}", username);

        var command = new DeleteSftpUserCommand(username);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to delete SFTP user {Username}: {Message}", 
                username, result.Message);
            return BadRequest(Error(result.Message));
        }

        _logger.LogInformation("Successfully deleted SFTP user: {Username}", username);
        return Success(new SftpOperationResponse { Success = true, Message = result.Message }, 
            result.Message);
    }

    // SFTP Directory Management Endpoints

    /// <summary>
    /// If a user exists in the SFTPUser table but not in SFTPGo, create it in SFTPGo and set up directories/mappings.
    /// </summary>
    /// 
    [HttpGet("sftp-users/{username}/SFTPGo-sync")] 
    [SwaggerOperation(Summary = "Ensure User Exists in SFTPGo", Description = "Creates the user in SFTPGo if missing, based on SFTPUser table entry. Also sets up directories and mappings.")]
    [SwaggerResponse(200, "User ensured in SFTPGo", typeof(ApiResponse<SftpOperationResponse>))]
    [AllowAnonymous]
    public async Task<IActionResult> EnsureUserExistsInSFTPGo(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(Error("username is required"));

        var result = await _mediator.Send(new EnsureSFTPGoUserFromDbCommand(username));
        if (result.Failed)
            return BadRequest(Error(result.Errors.FirstOrDefault() ?? "Failed"));

        return Success(new SftpOperationResponse { Success = true, Message = "User ensured and directories mapped" }, "User ensured and directories mapped");
    }

    /// <summary>
    /// Ensure a user's SFTP directories (root + standard subdirs) exist and are mapped.
    /// </summary>

    /// <summary>
    /// Ensure SFTP directories for user (Aegis Admin Only)
    /// </summary>
    [HttpGet("sftp-users/{username}/directories/ensure")]
    [SwaggerOperation(Summary = "Ensure SFTP Directories", Description = "Ensures user's SFTP directories exist (handled automatically by SFTPGo)")]
    [SwaggerResponse(200, "Directories ensured", typeof(ApiResponse<SftpOperationResponse>))]
    public async Task<IActionResult> EnsureUserDirectories(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(Error("username is required"));

        var result = await _mediator.Send(new EnsureUserDirectoriesCommand(username));
        if (result.Failed)
            return BadRequest(Error(result.Errors.FirstOrDefault() ?? "Failed"));

        return Success(new SftpOperationResponse { Success = true, Message = "Directories ensured" }, "Directories ensured");
    }
}

