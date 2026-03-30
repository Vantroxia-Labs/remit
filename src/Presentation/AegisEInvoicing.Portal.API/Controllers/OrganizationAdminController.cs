using Asp.Versioning;
using EInvoiceIntegrator.Application.Features.BusinessManagement.Commands;
using EInvoiceIntegrator.Application.Features.BusinessManagement.Queries;
using EInvoiceIntegrator.Application.Features.System.Queries;
using EInvoiceIntegrator.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EInvoiceIntegrator.API.Controllers;

/// <summary>
/// Organization admin controller for On-Premise deployments
/// This handles all business management functions that KMPG cannot manage in On-Premise mode
/// For example: MTN Nigeria admins managing their own users, businesses, etc.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organization/admin")]
[Authorize(Policy = "OrganizationAdminOnly")]
public class OrganizationAdminController(
    IMediator mediator,
    ILogger<OrganizationAdminController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<OrganizationAdminController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Check if this is an On-Premise deployment
    /// </summary>
    private async Task<bool> IsOnPremiseDeployment()
    {
        try
        {
            var systemStatus = await _mediator.Send(new GetSystemSetupStatusQuery());
            return systemStatus.DeploymentMode == DeploymentMode.OnPremise;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns error for SaaS deployments where this controller shouldn't be used
    /// </summary>
    private ActionResult SaaSRestricted()
    {
        return BadRequest("Organization admin functions are only available in On-Premise deployments. Use KMPG admin interface for SaaS deployments.");
    }

    #region Business Management (On-Premise Only)

    /// <summary>
    /// Gets all businesses in the organization (On-Premise only)
    /// </summary>
    [HttpGet("businesses")]
    [SwaggerOperation(
        Summary = "Retrieve all businesses in the organization",
        Description = "Returns a list of all businesses managed by the organization. This endpoint is only available in On-Premise deployments where organization admins manage their own businesses.",
        OperationId = "GetOrganizationBusinesses",
        Tags = new[] { "Organization Admin" }
    )]
    [SwaggerResponse(200, "Successfully retrieved the list of businesses", typeof(IEnumerable<BusinessSummaryDto>))]
    [SwaggerResponse(400, "Bad request - Organization admin functions are only available in On-Premise deployments")]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - Organization admin role required")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<IEnumerable<BusinessSummaryDto>>> GetOrganizationBusinesses()
    {
        try
        {
            if (!await IsOnPremiseDeployment())
            {
                return SaaSRestricted();
            }

            var query = new GetAllBusinessesQuery();
            var businesses = await _mediator.Send(query);
            return Ok(businesses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization businesses");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets business details (On-Premise only)
    /// </summary>
    [HttpGet("businesses/{businessId:guid}")]
    [SwaggerOperation(
        Summary = "Retrieve detailed information for a specific business",
        Description = "Returns comprehensive details about a specific business identified by its unique ID. This endpoint is only available in On-Premise deployments.",
        OperationId = "GetBusinessDetails",
        Tags = new[] { "Organization Admin" }
    )]
    [SwaggerResponse(200, "Successfully retrieved business details", typeof(BusinessDetailDto))]
    [SwaggerResponse(400, "Bad request - Organization admin functions are only available in On-Premise deployments")]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - Organization admin role required")]
    [SwaggerResponse(404, "Business not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<BusinessDetailDto>> GetBusinessDetails(Guid businessId)
    {
        try
        {
            if (!await IsOnPremiseDeployment())
            {
                return SaaSRestricted();
            }

            var query = new GetBusinessDetailsQuery { BusinessId = businessId };
            var result = await _mediator.Send(query);
            
            if (result == null)
            {
                return NotFound($"Business not found: {businessId}");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business details: {BusinessId}", businessId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets users for the organization (On-Premise only)
    /// </summary>
    [HttpGet("businesses/{businessId:guid}/users")]
    [SwaggerOperation(
        Summary = "Retrieve all users associated with a specific business",
        Description = "Returns a list of all users who have access to the specified business. This endpoint is only available in On-Premise deployments.",
        OperationId = "GetBusinessUsers",
        Tags = new[] { "Organization Admin" }
    )]
    [SwaggerResponse(200, "Successfully retrieved the list of business users", typeof(IEnumerable<BusinessUserDto>))]
    [SwaggerResponse(400, "Bad request - Organization admin functions are only available in On-Premise deployments")]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - Organization admin role required")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<IEnumerable<BusinessUserDto>>> GetBusinessUsers(Guid businessId)
    {
        try
        {
            if (!await IsOnPremiseDeployment())
            {
                return SaaSRestricted();
            }

            var query = new GetBusinessUsersQuery { BusinessId = businessId };
            var users = await _mediator.Send(query);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business users: {BusinessId}", businessId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Adds a user to the organization (On-Premise only)
    /// </summary>
    [HttpPost("businesses/{businessId:guid}/users")]
    [SwaggerOperation(
        Summary = "Add a user to a specific business",
        Description = "Associates an existing user with the specified business and assigns them a role. This endpoint is only available in On-Premise deployments.",
        OperationId = "AddUserToBusiness",
        Tags = new[] { "Organization Admin" }
    )]
    [SwaggerResponse(200, "Successfully added user to business")]
    [SwaggerResponse(400, "Bad request - Organization admin functions are only available in On-Premise deployments")]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - Organization admin role required")]
    [SwaggerResponse(404, "Business or user not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult> AddUserToBusiness(
        Guid businessId,
        [FromBody] AddUserToBusinessRequest request)
    {
        try
        {
            if (!await IsOnPremiseDeployment())
            {
                return SaaSRestricted();
            }

            var command = new AddUserToBusinessCommand
            {
                BusinessId = businessId,
                UserId = request.UserId,
                Role = request.Role
            };
            
            var result = await _mediator.Send(command);
            
            if (!result.Success)
            {
                return NotFound(result.Message);
            }

            return Ok(result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user to business: {BusinessId}", businessId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Removes a user from the organization (On-Premise only)
    /// </summary>
    [HttpDelete("businesses/{businessId:guid}/users/{userId:guid}")]
    [SwaggerOperation(
        Summary = "Remove a user from a specific business",
        Description = "Removes the association between a user and the specified business. This endpoint is only available in On-Premise deployments.",
        OperationId = "RemoveUserFromBusiness",
        Tags = new[] { "Organization Admin" }
    )]
    [SwaggerResponse(200, "Successfully removed user from business")]
    [SwaggerResponse(400, "Bad request - Organization admin functions are only available in On-Premise deployments")]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - Organization admin role required")]
    [SwaggerResponse(404, "Business or user not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult> RemoveUserFromBusiness(Guid businessId, Guid userId)
    {
        try
        {
            if (!await IsOnPremiseDeployment())
            {
                return SaaSRestricted();
            }

            var command = new RemoveUserFromBusinessCommand
            {
                BusinessId = businessId,
                UserId = userId
            };
            
            var result = await _mediator.Send(command);
            
            if (!result.Success)
            {
                return NotFound(result.Message);
            }

            return Ok(result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user from business: {BusinessId}", businessId);
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region Analytics and Reporting (On-Premise Only)

    /// <summary>
    /// Gets organization usage statistics (On-Premise only)
    /// </summary>
    [HttpGet("analytics/usage")]
    [SwaggerOperation(
        Summary = "Retrieve organization-wide usage statistics",
        Description = "Returns usage statistics for the entire organization within a specified date range. Defaults to the last month if no date range is provided. This endpoint is only available in On-Premise deployments.",
        OperationId = "GetOrganizationUsageStats",
        Tags = new[] { "Organization Admin" }
    )]
    [SwaggerResponse(200, "Successfully retrieved usage statistics", typeof(IEnumerable<BusinessUsageStats>))]
    [SwaggerResponse(400, "Bad request - Organization admin functions are only available in On-Premise deployments")]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - Organization admin role required")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<IEnumerable<BusinessUsageStats>>> GetOrganizationUsageStats(
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null)
    {
        try
        {
            if (!await IsOnPremiseDeployment())
            {
                return SaaSRestricted();
            }

            var from = fromDate ?? DateTimeOffset.UtcNow.AddMonths(-1);
            var to = toDate ?? DateTimeOffset.UtcNow;

            var query = new GetUsageStatsQuery
            {
                FromDate = from,
                ToDate = to
            };
            
            var stats = await _mediator.Send(query);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization usage statistics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Generates compliance report for the organization (On-Premise only)
    /// </summary>
    [HttpGet("businesses/{businessId:guid}/compliance-report")]
    [SwaggerOperation(
        Summary = "Generate a compliance report for a specific business",
        Description = "Creates and returns a comprehensive compliance report for the specified business. This endpoint is only available in On-Premise deployments.",
        OperationId = "GenerateComplianceReport",
        Tags = new[] { "Organization Admin" }
    )]
    [SwaggerResponse(200, "Successfully generated compliance report", typeof(BusinessComplianceReport))]
    [SwaggerResponse(400, "Bad request - Organization admin functions are only available in On-Premise deployments")]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - Organization admin role required")]
    [SwaggerResponse(404, "Business not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<BusinessComplianceReport>> GenerateComplianceReport(Guid businessId)
    {
        try
        {
            if (!await IsOnPremiseDeployment())
            {
                return SaaSRestricted();
            }

            var query = new GenerateComplianceReportQuery { BusinessId = businessId };
            var report = await _mediator.Send(query);
            
            if (report == null)
            {
                return NotFound($"Business not found: {businessId}");
            }
            
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance report for business: {BusinessId}", businessId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets organization dashboard statistics (On-Premise only)
    /// </summary>
    [HttpGet("dashboard/stats")]
    [SwaggerOperation(
        Summary = "Retrieve organization dashboard statistics",
        Description = "Returns aggregated statistics for the organization dashboard including key metrics and performance indicators. This endpoint is only available in On-Premise deployments.",
        OperationId = "GetOrganizationDashboardStats",
        Tags = new[] { "Organization Admin" }
    )]
    [SwaggerResponse(200, "Successfully retrieved dashboard statistics", typeof(KMPGDashboardStatsDto))]
    [SwaggerResponse(400, "Bad request - Organization admin functions are only available in On-Premise deployments")]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - Organization admin role required")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<KMPGDashboardStatsDto>> GetOrganizationDashboardStats()
    {
        try
        {
            if (!await IsOnPremiseDeployment())
            {
                return SaaSRestricted();
            }

            var query = new GetDashboardStatsQuery();
            var stats = await _mediator.Send(query);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization dashboard statistics");
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region License Information (Read-Only)

    /// <summary>
    /// Gets current license information (On-Premise only)
    /// Organization can view but not modify license
    /// </summary>
    [HttpGet("license/status")]
    [SwaggerOperation(
        Summary = "Retrieve current license information",
        Description = "Returns the current license status including validity, expiry date, and organization details. This is a read-only endpoint - organizations can view but not modify license information. Only available in On-Premise deployments.",
        OperationId = "GetLicenseStatus",
        Tags = new[] { "Organization Admin" }
    )]
    [SwaggerResponse(200, "Successfully retrieved license status", typeof(LicenseStatusDto))]
    [SwaggerResponse(400, "Bad request - Organization admin functions are only available in On-Premise deployments")]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - Organization admin role required")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<LicenseStatusDto>> GetLicenseStatus()
    {
        try
        {
            if (!await IsOnPremiseDeployment())
            {
                return SaaSRestricted();
            }

            var systemStatus = await _mediator.Send(new GetSystemSetupStatusQuery());
            
            return Ok(new LicenseStatusDto
            {
                IsValid = systemStatus.IsLicenseValid ?? false,
                ExpiryDate = systemStatus.LicenseExpiryDate,
                DaysUntilExpiry = systemStatus.LicenseExpiryDate?.Subtract(DateTimeOffset.UtcNow).Days,
                OrganizationName = systemStatus.OrganizationName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving license status");
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion
}

// DTOs specific to organization admin
public record LicenseStatusDto
{
    public bool IsValid { get; init; }
    public DateTimeOffset? ExpiryDate { get; init; }
    public int? DaysUntilExpiry { get; init; }
    public string? OrganizationName { get; init; }
}