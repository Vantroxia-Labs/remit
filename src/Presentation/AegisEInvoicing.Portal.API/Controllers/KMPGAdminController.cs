using Asp.Versioning;
using EInvoiceIntegrator.API.Attributes;
using EInvoiceIntegrator.Application.Features.BusinessManagement.Commands;
using EInvoiceIntegrator.Application.Features.BusinessManagement.Queries;
using EInvoiceIntegrator.Application.Features.BusinessOnboarding.Commands;
using EInvoiceIntegrator.Application.Features.BusinessOnboarding.Queries;
using EInvoiceIntegrator.Application.Features.SubscriptionKeys.Commands;
using EInvoiceIntegrator.Application.Features.SubscriptionKeys.Queries;
using EInvoiceIntegrator.Application.Features.System.Queries;
using EInvoiceIntegrator.Domain.Constants;
using EInvoiceIntegrator.Domain.Entities;
using EInvoiceIntegrator.Domain.Entities.BusinessManagement;
using EInvoiceIntegrator.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EInvoiceIntegrator.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/kmpg-admin")]
[Authorize(Policy = "KMPGAdminOnly")]
[RequireRole(RoleConstants.PlatformAdmin)]
public class KMPGAdminController(
    IMediator mediator,
    ILogger<KMPGAdminController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<KMPGAdminController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Check if KMPG can manage a specific function based on deployment mode
    /// </summary>
    private async Task<bool> CanKMPGManage(BusinessFunction function)
    {
        try
        {
            var systemStatus = await _mediator.Send(new GetSystemSetupStatusQuery());
            
            if (systemStatus.DeploymentMode == DeploymentMode.OnPremise)
            {
                // In On-Premise mode, KMPG can only manage subscriptions
                return function == BusinessFunction.SubscriptionManagement;
            }
            
            // In SaaS mode, KMPG can manage everything
            return true;
        }
        catch
        {
            // If we can't determine deployment mode, default to restricted access
            return function == BusinessFunction.SubscriptionManagement;
        }
    }

    /// <summary>
    /// Returns a standardized "not authorized" response for On-Premise restrictions
    /// </summary>
    private ActionResult OnPremiseRestricted(string action)
    {
        return Forbid($"KMPG cannot {action} in On-Premise deployments. This function is managed by the organization.");
    }

    #region Business Onboarding Management

    /// <summary>
    /// Gets all pending business onboarding requests
    /// Note: Only available in SaaS deployments
    /// </summary>
    [HttpGet("onboarding/pending")]
    [SwaggerOperation(
        Summary = "Get pending business onboarding requests",
        Description = "Retrieves all pending business onboarding requests awaiting KMPG review. Only available in SaaS deployment mode.",
        OperationId = "GetPendingOnboardings",
        Tags = new[] { "KMPG Admin - Business Onboarding" }
    )]
    [SwaggerResponse(200, "Pending onboarding requests retrieved successfully", typeof(IEnumerable<BusinessOnboardingDto>))]
    [SwaggerResponse(403, "Operation forbidden in On-Premise deployment mode")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<IEnumerable<BusinessOnboardingDto>>> GetPendingOnboardings()
    {
        try
        {
            if (!await CanKMPGManage(BusinessFunction.BusinessOnboarding))
            {
                return OnPremiseRestricted("manage business onboarding");
            }

            var query = new GetPendingOnboardingsQuery();
            var onboardings = await _mediator.Send(query);
            return Ok(onboardings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending onboardings");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific business onboarding request
    /// </summary>
    [HttpGet("onboarding/{onboardingId:guid}")]
    [SwaggerOperation(
        Summary = "Get specific business onboarding request",
        Description = "Retrieves detailed information about a specific business onboarding request by ID.",
        OperationId = "GetOnboarding",
        Tags = new[] { "KMPG Admin - Business Onboarding" }
    )]
    [SwaggerResponse(200, "Onboarding request retrieved successfully", typeof(BusinessOnboardingDto))]
    [SwaggerResponse(404, "Onboarding request not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<BusinessOnboardingDto>> GetOnboarding(Guid onboardingId)
    {
        try
        {
            var query = new GetOnboardingStatusQuery { OnboardingId = onboardingId };
            var result = await _mediator.Send(query);
            
            if (result == null)
            {
                return NotFound($"Onboarding request not found: {onboardingId}");
            }

            return Ok(new BusinessOnboardingDto
            {
                Id = result.OnboardingId,
                CompanyName = result.CompanyName,
                Status = result.Status,
                StatusReason = result.StatusReason,
                StatusLastChanged = result.StatusLastChanged,
                CreatedAt = result.SubmittedAt,
                CreatedBusinessId = result.CreatedBusinessId
                // Note: Some fields might be missing from the query result
                // You may need to create a more detailed query if needed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving onboarding: {OnboardingId}", onboardingId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Assigns a KMPG reviewer to an onboarding request
    /// </summary>
    [HttpPost("onboarding/{onboardingId:guid}/assign-reviewer")]
    [SwaggerOperation(
        Summary = "Assign KMPG reviewer to onboarding request",
        Description = "Assigns a KMPG staff member as the reviewer for a business onboarding request.",
        OperationId = "AssignReviewer",
        Tags = new[] { "KMPG Admin - Business Onboarding" }
    )]
    [SwaggerResponse(200, "Reviewer assigned successfully")]
    [SwaggerResponse(404, "Onboarding request not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult> AssignReviewer(
        Guid onboardingId,
        [FromBody] AssignReviewerRequest request)
    {
        try
        {
            var command = new AssignReviewerCommand
            {
                OnboardingId = onboardingId,
                ReviewerId = request.ReviewerId,
                Notes = request.Notes
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
            _logger.LogError(ex, "Error assigning reviewer to onboarding: {OnboardingId}", onboardingId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Approves a business onboarding request
    /// </summary>
    [HttpPost("onboarding/{onboardingId:guid}/approve")]
    [SwaggerOperation(
        Summary = "Approve business onboarding request",
        Description = "Approves a business onboarding request and creates the business account.",
        OperationId = "ApproveOnboarding",
        Tags = new[] { "KMPG Admin - Business Onboarding" }
    )]
    [SwaggerResponse(200, "Onboarding request approved successfully")]
    [SwaggerResponse(404, "Onboarding request not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult> ApproveOnboarding(
        Guid onboardingId,
        [FromBody] ApproveOnboardingRequest request)
    {
        try
        {
            var command = new ApproveOnboardingCommand
            {
                OnboardingId = onboardingId,
                ApprovalNotes = request.ApprovalNotes
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
            _logger.LogError(ex, "Error approving onboarding: {OnboardingId}", onboardingId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Rejects a business onboarding request
    /// </summary>
    [HttpPost("onboarding/{onboardingId:guid}/reject")]
    [SwaggerOperation(
        Summary = "Reject business onboarding request",
        Description = "Rejects a business onboarding request with a specified reason.",
        OperationId = "RejectOnboarding",
        Tags = new[] { "KMPG Admin - Business Onboarding" }
    )]
    [SwaggerResponse(200, "Onboarding request rejected successfully")]
    [SwaggerResponse(404, "Onboarding request not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult> RejectOnboarding(
        Guid onboardingId,
        [FromBody] RejectOnboardingRequest request)
    {
        try
        {
            var command = new RejectOnboardingCommand
            {
                OnboardingId = onboardingId,
                RejectionReason = request.RejectionReason
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
            _logger.LogError(ex, "Error rejecting onboarding: {OnboardingId}", onboardingId);
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region Business Management

    /// <summary>
    /// Gets all businesses managed by KMPG
    /// </summary>
    [HttpGet("businesses")]
    [SwaggerOperation(
        Summary = "Get all businesses",
        Description = "Retrieves all businesses managed by KMPG, optionally filtered by status.",
        OperationId = "GetAllBusinesses",
        Tags = new[] { "KMPG Admin - Business Management" }
    )]
    [SwaggerResponse(200, "Businesses retrieved successfully", typeof(IEnumerable<BusinessSummaryDto>))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<IEnumerable<BusinessSummaryDto>>> GetAllBusinesses(
        [FromQuery] BusinessStatus? status = null)
    {
        try
        {
            var query = new GetAllBusinessesQuery { StatusFilter = status };
            var businesses = await _mediator.Send(query);
            return Ok(businesses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving businesses");
            return StatusCode(500, "Internal server error");
        }
    }
    
    /*
    // ORIGINAL METHOD - TO BE CONVERTED TO CQRS
    public async Task<ActionResult<IEnumerable<BusinessSummaryDto>>> GetAllBusinesses_OLD(
        [FromQuery] BusinessStatus? status = null)
    {
        try
        {
            // var businesses = status.HasValue 
            //     ? await _businessManagementService.GetBusinessesByStatusAsync(status.Value)
            //     : await _businessManagementService.GetAllBusinessesAsync();
            // return Ok(businesses.Select(MapBusinessToSummaryDto));
            return Ok(new List<BusinessSummaryDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving businesses");
            return StatusCode(500, "Internal server error");
        }
    }
    */

    /// <summary>
    /// Gets detailed information about a specific business - TODO: Convert to CQRS
    /// </summary>
    [HttpGet("businesses/{businessId:guid}")]
    [SwaggerOperation(
        Summary = "Get business details",
        Description = "Retrieves detailed information about a specific business including subscription and FIRS integration status.",
        OperationId = "GetBusiness",
        Tags = new[] { "KMPG Admin - Business Management" }
    )]
    [SwaggerResponse(200, "Business details retrieved successfully", typeof(BusinessDetailDto))]
    [SwaggerResponse(404, "Business not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<BusinessDetailDto>> GetBusiness(Guid businessId)
    {
        try
        {
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
    
    /*
    public async Task<ActionResult<BusinessDetailDto>> GetBusiness_OLD(Ulid businessId)
    {
        try
        {
            // var business = await _businessManagementService.GetBusinessAsync(businessId);
            // if (business == null)
            // {
            //     return NotFound($"Business not found: {businessId}");
            // }
            // var subscriptionInfo = await _businessManagementService.GetBusinessSubscriptionAsync(businessId);
            // var firsStatus = await _businessManagementService.GetBusinessFIRSStatusAsync(businessId);
            // var users = await _businessManagementService.GetBusinessUsersAsync(businessId);

            // return Ok(new BusinessDetailDto
            // {
            //     Id = business.Id,
            //     Name = business.Name,
            //     Description = business.Description,
            //     BusinessRegistrationNumber = business.BusinessRegistrationNumber,
            //     TIN = business.TaxIdentificationNumber.Value,
            //     ContactEmail = business.ContactEmail,
            //     ContactPhone = business.ContactPhone,
            //     Status = business.Status,
            //     IsActive = business.IsActive,
            //     CreatedAt = business.CreatedAt,
            //     LastFIRSSync = business.LastFIRSSync,
            //     SubscriptionInfo = subscriptionInfo,
            //     FIRSStatus = firsStatus,
            //     UserCount = users.Count()
            // });
            return StatusCode(501, "Not implemented - converting to CQRS");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business details: {BusinessId}", businessId);
            return StatusCode(500, "Internal server error");
        }
    }
    */

    /// <summary>
    /// Suspends a business
    /// </summary>
    [HttpPost("businesses/{businessId:guid}/suspend")]
    [SwaggerOperation(
        Summary = "Suspend business account",
        Description = "Suspends a business account, preventing access to system features. A reason must be provided.",
        OperationId = "SuspendBusiness",
        Tags = new[] { "KMPG Admin - Business Management" }
    )]
    [SwaggerResponse(200, "Business suspended successfully")]
    [SwaggerResponse(404, "Business not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult> SuspendBusiness(
        Guid businessId,
        [FromBody] SuspendBusinessRequest request)
    {
        try
        {
            var command = new SuspendBusinessCommand
            {
                BusinessId = businessId,
                Reason = request.Reason
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
            _logger.LogError(ex, "Error suspending business: {BusinessId}", businessId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Reactivates a suspended business
    /// </summary>
    [HttpPost("businesses/{businessId:guid}/reactivate")]
    [SwaggerOperation(
        Summary = "Reactivate suspended business",
        Description = "Reactivates a previously suspended business account, restoring access to system features.",
        OperationId = "ReactivateBusiness",
        Tags = new[] { "KMPG Admin - Business Management" }
    )]
    [SwaggerResponse(200, "Business reactivated successfully")]
    [SwaggerResponse(404, "Business not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult> ReactivateBusiness(
        Guid businessId,
        [FromBody] ReactivateBusinessRequest request)
    {
        try
        {
            var command = new ReactivateBusinessCommand
            {
                BusinessId = businessId,
                Reason = request.Reason
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
            _logger.LogError(ex, "Error reactivating business: {BusinessId}", businessId);
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region Subscription Management

    /// <summary>
    /// Gets businesses with expired subscriptions
    /// </summary>
    [HttpGet("subscriptions/expired")]
    [SwaggerOperation(
        Summary = "Get businesses with expired subscriptions",
        Description = "Retrieves all businesses that have expired subscriptions requiring renewal.",
        OperationId = "GetExpiredSubscriptions",
        Tags = new[] { "KMPG Admin - Subscription Management" }
    )]
    [SwaggerResponse(200, "Expired subscriptions retrieved successfully", typeof(IEnumerable<ExpiredSubscriptionDto>))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<IEnumerable<ExpiredSubscriptionDto>>> GetExpiredSubscriptions()
    {
        try
        {
            var query = new GetExpiredSubscriptionsQuery();
            var expiredSubscriptions = await _mediator.Send(query);
            return Ok(expiredSubscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expired subscriptions");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates a business subscription tier
    /// </summary>
    [HttpPut("businesses/{businessId:guid}/subscription")]
    [SwaggerOperation(
        Summary = "Update business subscription tier",
        Description = "Updates the subscription tier for a business (e.g., Basic, Standard, Premium).",
        OperationId = "UpdateBusinessSubscription",
        Tags = new[] { "KMPG Admin - Subscription Management" }
    )]
    [SwaggerResponse(200, "Subscription updated successfully")]
    [SwaggerResponse(404, "Business not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult> UpdateBusinessSubscription(
        Guid businessId,
        [FromBody] UpdateSubscriptionRequest request)
    {
        try
        {
            var command = new UpdateBusinessSubscriptionCommand
            {
                BusinessId = businessId,
                NewTier = request.NewTier
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
            _logger.LogError(ex, "Error updating subscription for business: {BusinessId}", businessId);
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region Analytics and Reporting

    /// <summary>
    /// Gets usage statistics for all businesses
    /// </summary>
    [HttpGet("analytics/usage")]
    [SwaggerOperation(
        Summary = "Get usage statistics for all businesses",
        Description = "Retrieves usage statistics for all businesses within a specified date range. Defaults to the last 30 days.",
        OperationId = "GetUsageStats",
        Tags = new[] { "KMPG Admin - Analytics & Reporting" }
    )]
    [SwaggerResponse(200, "Usage statistics retrieved successfully", typeof(IEnumerable<BusinessUsageStats>))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<IEnumerable<BusinessUsageStats>>> GetUsageStats(
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null)
    {
        try
        {
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
            _logger.LogError(ex, "Error retrieving usage statistics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Generates a compliance report for a business
    /// </summary>
    [HttpGet("businesses/{businessId:guid}/compliance-report")]
    [SwaggerOperation(
        Summary = "Generate compliance report for a business",
        Description = "Generates a comprehensive compliance report including FIRS connection status, subscription validity, and audit information.",
        OperationId = "GenerateComplianceReport",
        Tags = new[] { "KMPG Admin - Analytics & Reporting" }
    )]
    [SwaggerResponse(200, "Compliance report generated successfully", typeof(BusinessComplianceReport))]
    [SwaggerResponse(404, "Business not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<BusinessComplianceReport>> GenerateComplianceReport(Guid businessId)
    {
        try
        {
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
    /// Gets system-wide dashboard statistics
    /// </summary>
    [HttpGet("dashboard/stats")]
    [SwaggerOperation(
        Summary = "Get system-wide dashboard statistics",
        Description = "Retrieves aggregated statistics for the KMPG admin dashboard including total businesses, active/suspended counts, and pending onboardings.",
        OperationId = "GetDashboardStats",
        Tags = new[] { "KMPG Admin - Analytics & Reporting" }
    )]
    [SwaggerResponse(200, "Dashboard statistics retrieved successfully", typeof(KMPGDashboardStatsDto))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<KMPGDashboardStatsDto>> GetDashboardStats()
    {
        try
        {
            var query = new GetDashboardStatsQuery();
            var stats = await _mediator.Send(query);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard statistics");
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region User Management

    /// <summary>
    /// Gets users for a specific business
    /// </summary>
    [HttpGet("businesses/{businessId:guid}/users")]
    [SwaggerOperation(
        Summary = "Get users for a specific business",
        Description = "Retrieves all users associated with a specific business account.",
        OperationId = "GetBusinessUsers",
        Tags = new[] { "KMPG Admin - User Management" }
    )]
    [SwaggerResponse(200, "Business users retrieved successfully", typeof(IEnumerable<BusinessUserDto>))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<IEnumerable<BusinessUserDto>>> GetBusinessUsers(Guid businessId)
    {
        try
        {
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
    /// Adds a user to a business
    /// </summary>
    [HttpPost("businesses/{businessId:guid}/users")]
    [SwaggerOperation(
        Summary = "Add user to business",
        Description = "Adds an existing user to a business account with a specified role.",
        OperationId = "AddUserToBusiness",
        Tags = new[] { "KMPG Admin - User Management" }
    )]
    [SwaggerResponse(200, "User added to business successfully")]
    [SwaggerResponse(404, "Business or user not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult> AddUserToBusiness(
        Guid businessId,
        [FromBody] AddUserToBusinessRequest request)
    {
        try
        {
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
    /// Removes a user from a business
    /// </summary>
    [HttpDelete("businesses/{businessId:guid}/users/{userId:guid}")]
    [SwaggerOperation(
        Summary = "Remove user from business",
        Description = "Removes a user's access to a specific business account.",
        OperationId = "RemoveUserFromBusiness",
        Tags = new[] { "KMPG Admin - User Management" }
    )]
    [SwaggerResponse(200, "User removed from business successfully")]
    [SwaggerResponse(404, "Business or user not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult> RemoveUserFromBusiness(Guid businessId, Guid userId)
    {
        try
        {
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

    #region Helper Methods

    private static BusinessOnboardingDto MapOnboardingToDto(BusinessOnboarding onboarding)
    {
        return new BusinessOnboardingDto
        {
            Id = onboarding.Id,
            CompanyName = onboarding.CompanyName,
            BusinessRegistrationNumber = onboarding.BusinessRegistrationNumber,
            TIN = onboarding.TaxIdentificationNumber.Value,
            ContactEmail = onboarding.ContactEmail,
            ContactPhone = onboarding.ContactPhone,
            ContactPersonName = onboarding.ContactPersonName,
            ContactPersonTitle = onboarding.ContactPersonTitle,
            DeploymentType = onboarding.DeploymentType,
            Status = onboarding.Status,
            StatusReason = onboarding.StatusReason,
            StatusLastChanged = onboarding.StatusLastChanged,
            AssignedKMPGReviewer = onboarding.AssignedKMPGReviewer,
            ReviewStartedAt = onboarding.ReviewStartedAt,
            ReviewCompletedAt = onboarding.ReviewCompletedAt,
            ReviewNotes = onboarding.ReviewNotes,
            RiskAssessment = onboarding.RiskAssessment,
            ExpectedMonthlyInvoices = onboarding.ExpectedMonthlyInvoices,
            ExpectedUsers = onboarding.ExpectedUsers,
            HasFIRSCredentials = onboarding.HasFIRSCredentials,
            CreatedAt = onboarding.CreatedAt,
            CreatedBusinessId = onboarding.CreatedBusinessId
        };
    }

    private static BusinessSummaryDto MapBusinessToSummaryDto(Business business)
    {
        return new BusinessSummaryDto
        {
            Id = business.Id,
            Name = business.Name,
            TIN = business.TaxIdentificationNumber.Value,
            ContactEmail = business.ContactEmail,
            Status = business.Status,
            IsActive = business.IsActive,
            IsConnectedToFIRS = business.IsConnectedToFIRS,
            LastFIRSSync = business.LastFIRSSync,
            SubscriptionTier = business.Subscription.Tier,
            SubscriptionStatus = business.Subscription.Status,
            SubscriptionEndDate = business.Subscription.EndDate,
            CreatedAt = business.CreatedAt
        };
    }

    #endregion

    #region Subscription Key Management

    /// <summary>
    /// Generates a new subscription key for on-premise deployment
    /// </summary>
    [HttpPost("subscription-keys")]
    [SwaggerOperation(
        Summary = "Generate subscription key for on-premise deployment",
        Description = "Generates a new subscription key for on-premise deployments with specified features and limits.",
        OperationId = "GenerateSubscriptionKey",
        Tags = new[] { "KMPG Admin - Subscription Keys" }
    )]
    [SwaggerResponse(200, "Subscription key generated successfully", typeof(SubscriptionKeyResponse))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<SubscriptionKeyResponse>> GenerateSubscriptionKey(
        [FromBody] GenerateSubscriptionKeyRequest request)
    {
        try
        {
            var command = new GenerateSubscriptionKeyCommand
            {
                BusinessName = request.BusinessName,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                ExpiryDate = request.ExpiryDate,
                MaxUsers = request.MaxUsers,
                MaxBusinesses = request.MaxBusinesses,
                Features = request.Features
            };

            var result = await _mediator.Send(command);

            return Ok(new SubscriptionKeyResponse
            {
                SubscriptionKeyId = result.SubscriptionKeyId,
                Key = result.Key,
                BusinessName = result.BusinessName,
                ExpiryDate = result.ExpiryDate,
                CreatedAt = result.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when generating subscription key");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating subscription key");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Validates a subscription key
    /// </summary>
    [HttpPost("subscription-keys/validate")]
    [SwaggerOperation(
        Summary = "Validate subscription key",
        Description = "Validates a subscription key and returns its details including expiry date, user limits, and features.",
        OperationId = "ValidateSubscriptionKey",
        Tags = new[] { "KMPG Admin - Subscription Keys" }
    )]
    [SwaggerResponse(200, "Subscription key validated successfully", typeof(SubscriptionKeyValidationResponse))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<SubscriptionKeyValidationResponse>> ValidateSubscriptionKey(
        [FromBody] ValidateSubscriptionKeyRequest request)
    {
        try
        {
            var query = new ValidateSubscriptionKeyQuery { Key = request.Key };
            var result = await _mediator.Send(query);

            return Ok(new SubscriptionKeyValidationResponse
            {
                IsValid = result.IsValid,
                BusinessName = result.BusinessName,
                ContactEmail = result.ContactEmail,
                ExpiryDate = result.ExpiryDate,
                MaxUsers = result.MaxUsers,
                MaxBusinesses = result.MaxBusinesses,
                Features = result.Features,
                ValidationError = result.ValidationError
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating subscription key");
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion
}

// DTOs for KMPG Admin Portal
public record BusinessOnboardingDto
{
    public Guid Id { get; init; }
    public string CompanyName { get; init; } = default!;
    public string BusinessRegistrationNumber { get; init; } = default!;
    public string TIN { get; init; } = default!;
    public string ContactEmail { get; init; } = default!;
    public string ContactPhone { get; init; } = default!;
    public string ContactPersonName { get; init; } = default!;
    public string ContactPersonTitle { get; init; } = default!;
    public BusinessDeploymentType DeploymentType { get; init; }
    public BusinessOnboardingStatus Status { get; init; }
    public string? StatusReason { get; init; }
    public DateTimeOffset? StatusLastChanged { get; init; }
    public Guid? AssignedKMPGReviewer { get; init; }
    public DateTimeOffset? ReviewStartedAt { get; init; }
    public DateTimeOffset? ReviewCompletedAt { get; init; }
    public string? ReviewNotes { get; init; }
    public BusinessRiskAssessment RiskAssessment { get; init; }
    public int ExpectedMonthlyInvoices { get; init; }
    public int ExpectedUsers { get; init; }
    public bool HasFIRSCredentials { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? CreatedBusinessId { get; init; }
}

public record BusinessSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string TIN { get; init; } = default!;
    public string ContactEmail { get; init; } = default!;
    public BusinessStatus Status { get; init; }
    public bool IsActive { get; init; }
    public bool IsConnectedToFIRS { get; init; }
    public DateTimeOffset? LastFIRSSync { get; init; }
    public SubscriptionTier SubscriptionTier { get; init; }
    public SubscriptionStatus SubscriptionStatus { get; init; }
    public DateTimeOffset SubscriptionEndDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public record BusinessDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string BusinessRegistrationNumber { get; init; } = default!;
    public string TIN { get; init; } = default!;
    public string ContactEmail { get; init; } = default!;
    public string ContactPhone { get; init; } = default!;
    public BusinessStatus Status { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastFIRSSync { get; init; }
    public BusinessSubscriptionInfo? SubscriptionInfo { get; init; }
    public FIRSIntegrationStatus? FIRSStatus { get; init; }
    public int UserCount { get; init; }
}

public record BusinessUserDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public bool IsActive { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public record KMPGDashboardStats
{
    public int TotalBusinesses { get; init; }
    public int ActiveBusinesses { get; init; }
    public int SuspendedBusinesses { get; init; }
    public int PendingOnboardings { get; init; }
    public int ExpiredSubscriptions { get; init; }
    public int SaaSBusinesses { get; init; }
    public int OnPremiseBusinesses { get; init; }
}

// Supporting DTOs
public record BusinessSubscriptionInfo
{
    public Guid BusinessId { get; init; }
    public string BusinessName { get; init; } = default!;
    public SubscriptionTier SubscriptionTier { get; init; }
    public SubscriptionStatus SubscriptionStatus { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset EndDate { get; init; }
    public int MonthlyInvoiceLimit { get; init; }
    public int UserLimit { get; init; }
    public bool IsActive { get; init; }
}

public record FIRSIntegrationStatus
{
    public Guid BusinessId { get; init; }
    public bool IsConnected { get; init; }
    public DateTimeOffset? LastConnectionTest { get; init; }
    public string ConnectionStatus { get; init; } = default!;
    public DateTimeOffset? LastSync { get; init; }
    public bool HasValidToken { get; init; }
}

public record BusinessUsageStats
{
    public Guid BusinessId { get; init; }
    public string BusinessName { get; init; } = default!;
    public string Period { get; init; } = default!;
    public int InvoicesProcessed { get; init; }
    public string SubscriptionTier { get; init; } = default!;
    public bool IsWithinLimits { get; init; }
}

public record BusinessComplianceReport
{
    public Guid BusinessId { get; init; }
    public string BusinessName { get; init; } = default!;
    public DateTimeOffset ReportGeneratedAt { get; init; }
    public int ComplianceScore { get; init; }
    public bool HasValidFIRSConnection { get; init; }
    public bool HasActiveSubscription { get; init; }
    public DateTimeOffset? LastAuditDate { get; init; }
    public List<string> Issues { get; init; } = [];
    public List<string> Recommendations { get; init; } = [];
}

// Request DTOs
public record AssignReviewerRequest
{
    public Guid ReviewerId { get; init; }
    public string? Notes { get; init; }
}

public record ApproveOnboardingRequest
{
    public string? ApprovalNotes { get; init; }
}

public record RejectOnboardingRequest
{
    public string RejectionReason { get; init; } = default!;
}

public record SuspendBusinessRequest
{
    public string Reason { get; init; } = default!;
}

public record ReactivateBusinessRequest
{
    public string Reason { get; init; } = default!;
}

public record UpdateSubscriptionRequest
{
    public SubscriptionTier NewTier { get; init; }
}

public record AddUserToBusinessRequest
{
    public Guid UserId { get; init; }
    public string Role { get; init; } = default!;
}

// Subscription Key DTOs
public record GenerateSubscriptionKeyRequest
{
    public string BusinessName { get; init; } = default!;
    public string ContactEmail { get; init; } = default!;
    public string? ContactPhone { get; init; }
    public DateTimeOffset ExpiryDate { get; init; }
    public int MaxUsers { get; init; } = 10;
    public int MaxBusinesses { get; init; } = 1;
    public string? Features { get; init; }
}

public record SubscriptionKeyResponse
{
    public Guid SubscriptionKeyId { get; init; }
    public string Key { get; init; } = default!;
    public string BusinessName { get; init; } = default!;
    public DateTimeOffset ExpiryDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public record ValidateSubscriptionKeyRequest
{
    public string Key { get; init; } = default!;
}

public record SubscriptionKeyValidationResponse
{
    public bool IsValid { get; init; }
    public string? BusinessName { get; init; }
    public string? ContactEmail { get; init; }
    public DateTimeOffset? ExpiryDate { get; init; }
    public int? MaxUsers { get; init; }
    public int? MaxBusinesses { get; init; }
    public string? Features { get; init; }
    public string? ValidationError { get; init; }
}