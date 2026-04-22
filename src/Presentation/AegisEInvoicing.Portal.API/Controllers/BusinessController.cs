using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.Business.Request;
using AegisEInvoicing.Portal.API.Models.Business.Response;
using AegisEInvoicing.Portal.API.Models.BusinessOnboarding.Response;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.AddFirsApiConfiguration;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.AddQrCodeConfiguration;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.ActivateBusiness;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.SuspendBusiness;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.UpdateBusiness;
using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using AegisEInvoicing.Application.Features.BusinessManagement.Queries;
using AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetBusinesses;
using AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetFirsApiConfiguration;
using AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetQrCodeConfiguration;
using AegisEInvoicing.Application.Features.BusinessManagement.Queries.ValidateBusiness;
using AegisEInvoicing.Application.Features.BusinessOnboarding.Queries.GetBusinessById;
using AegisEInvoicing.Application.Features.DashboardAnalytics.DTOs;
using AegisEInvoicing.Application.Features.DashboardAnalytics.Queries;
using AegisEInvoicing.Application.Features.Miscellenous.DTOs;
using AegisEInvoicing.Application.Features.Miscellenous.Queries;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Models;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;


namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for business onboarding to EInvoice Integrator - KMPG Platform Admin Only
/// KMPG onboards businesses for project usage and manages all FIRS interactions for SaaS/API solutions
/// All business onboarding and subscription activation is managed exclusively by KMPG platform administrators
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Authorize]
public partial class BusinessController(
    IMediator mediator,
    ILogger<BusinessController> logger,
    IConfiguration configuration,
    ICurrentUserService currentUserService) : BaseApiController
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<BusinessController> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly ICurrentUserService _currentUserService = currentUserService;


    /// <summary>   
    /// Update business to EInvoice Integrator platform (KMPG Platform Admin only)
    /// </summary>
    /// <param name="request">Business update details</param>
    /// <param name="businessId">Business Id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result with subscription status</returns>
    /// <returns></returns>
    [HttpPut("update-business/{businessId}")]
    [RequireRole(RoleConstants.AegisAdmin)]
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

        var command = new UpdateBusinessCommand(
            businessId,
            address,
            request.InvoicePrefix ?? string.Empty,
            request.Industry,
            request.ContactEmail,
            request.ContactPhone,
            request.Description,
            request.ServiceId,
            request.BusinessRegistrationNumber,
            request.TaxIdentificationNumber,
            !string.IsNullOrWhiteSpace(request.NRSBusinessId) && Guid.TryParse(request.NRSBusinessId, out var parsedNrsId)
                ? parsedNrsId
                : null);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to update business to platform: {Message}", result.Message);
            return Error(result.Message);
        }

        _logger.LogInformation("KMPG successfully update business to EInvoice Integrator platform with {Message}", result.Message);

        var response = new UpdateBusinessResponse
        {
            Status = result.IsSuccess!,
            Message = result.Message
        };

        return Success(response, "Business successfully updated to EInvoice Integrator platform");
    }

    /// <summary>
    /// Update FIRS API Credentials to current business
    /// </summary>
    /// <param name="request">Configuration assignment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Assignment result</returns>
    [HttpPatch("update-firs-credentials")]
    [HttpPatch("update-NRS-credentials")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> UpdateFirsCredentials(
        [FromBody] UpdateFirsCredentialsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new AddFirsApiConfigurationCommand(request.FirsApiKey, request.FirsClientSecret);
            var result = await Mediator.Send(command, cancellationToken);
            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning FIRS API configuration to business");
            return Error("An error occurred while assigning the configuration", StatusCodes.Status500InternalServerError);
        }
    }


    /// <summary>
    /// Update QR Code Configuration to current business
    /// </summary>
    /// <param name="request">Configuration assignment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Assignment result</returns>
    [HttpPatch("update-qrcode-configuration")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> AddQrCodeConfigurationCommand(
        [FromBody] UpdateQrCodeConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new AddQrCodeConfigurationCommand(request.PublicKey, request.Certificate);
            var result = await Mediator.Send(command, cancellationToken);
            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning QRCode configuration to business");
            return Error("An error occurred while assigning the configuration", StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("get-firs-credentials")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetFirsCredentials(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("fetch Firs credentials for business");

        var request = new GetFirsApiConfigurationQuery();

        var result = await _mediator.Send(request, cancellationToken);
        return GenericResponse(result.Message, result.IsSuccess,
            result.StatusCodes,
            result.FirsApiConfiguration);
    }

    [HttpGet("get-qrcode-configuration")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetQrCodeConfiguration(
       CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("fetch QR Code credentials for business");

        var request = new GetQrCodeConfigurationQuery();

        var result = await _mediator.Send(request, cancellationToken);
        return GenericResponse(result.Message, result.IsSuccess,
                              result.StatusCodes);
    }

    /// <summary>   
    /// Update business to EInvoice Integrator platform (KMPG Platform Admin only)
    /// </summary>
    /// <param name="request">Business update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result with subscription status</returns>
    /// <returns></returns>
    [HttpGet]
    [RequireRole(RoleConstants.AegisAdmin)]
    public async Task<IActionResult> GetBusinesses([FromQuery] GetBusinessesQuery request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("KMPG fetch businesses to EInvoice Integrator platform");

        var result = await _mediator.Send(request, cancellationToken);

        return Success(result, "List of businesses");
    }

    /// <summary>
    /// Retrieves dashboard analytics statistics for the authenticated business or all businesses (Aegis Admin)
    /// </summary>
    /// <param name="request">Analytics query parameters including date range filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard analytics data with invoice counts and amounts</returns>
    [HttpGet("dashboard-analytics")]
    [MapToApiVersion("1.0")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetDashboardAnalytics(
        [FromQuery] GetDashboardAnalyticsQuery request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching dashboard analytics for business");

        var result = await _mediator.Send(request, cancellationToken);

        return Success(result, "Dashboard analytics retrieved successfully");
    }

    /// <summary>
    /// Retrieves V2 dashboard analytics with 12-month data for General or VATTable dashboards
    /// </summary>
    /// <param name="dashboardType">Dashboard type: General (1) or VATTable (2)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard analytics data with 12-month historical data</returns>
    [HttpGet("dashboard-analytics")]
    [MapToApiVersion("2.0")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetDashboardAnalyticsV2(
        [FromQuery] DashboardType dashboardType = DashboardType.General,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching V2 dashboard analytics for dashboard type: {DashboardType}", dashboardType);

        var query = new GetDashboardAnalyticsV2Query(dashboardType);
        var result = await _mediator.Send(query, cancellationToken);

        return Success(result, $"{dashboardType} dashboard analytics retrieved successfully");
    }

    /// <summary>   
    /// Fetches a single business to EInvoice Integrator platform (KMPG Platform Admin and Merchant Admin only)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="businessId">Business Id</param>
    /// <returns></returns>
    [HttpGet("fetch-business")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetBusiness([FromQuery] Guid? businessId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("fetch business to EInvoice Integrator platform");

        var request = new GetBusinessByIdQuery(businessId);

        var result = await _mediator.Send(request, cancellationToken);

        if (result is null)
            return Error("Business Not Found", 404);

        return Success(result, string.Empty);
    }

    /// <summary>
    /// Validate business fields for existence (KMPG Platform Admin only)
    /// </summary>
    /// <param name="request">Validation request containing field types and values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation results indicating whether each field exists</returns>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateBusinessFieldsAsync(
        [FromBody] BusinessValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("KMPG platform admin requested business field validation for {FieldCount} fields",
            request.ValidationFields?.Count ?? 0);

        if (request.ValidationFields == null || !request.ValidationFields.Any())
        {
            return BadRequest(Error("At least one validation field is required"));
        }

        var query = new ValidateBusinessQuery(request.ValidationFields);
        var validationResults = await _mediator.Send(query, cancellationToken);

        var existingFields = validationResults.Where(r => r.Value).Select(r => r.Key).ToList();
        var nonExistingFields = validationResults.Where(r => !r.Value).Select(r => r.Key).ToList();

        var message = "Validation completed";
        if (existingFields.Any())
        {
            message += $". Found existing: {string.Join(", ", existingFields)}";
        }
        if (nonExistingFields.Any())
        {
            message += $". Not found: {string.Join(", ", nonExistingFields)}";
        }

        var response = new BusinessValidationResponse
        {
            ValidationResults = validationResults,
            Message = message
        };

        _logger.LogInformation("Business field validation completed. Existing fields: {ExistingCount}, Non-existing fields: {NonExistingCount}",
            existingFields.Count, nonExistingFields.Count);

        return Success(response, message);
    }

    [HttpGet("all-business-roles")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<PlatformBusinessRoleSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<PlatformBusinessRoleSummaryDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<PlatformBusinessRoleSummaryDto>>), StatusCodes.Status403Forbidden)]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.AegisAdmin)]
    public async Task<IActionResult> GetBusinessRoles(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching list of business roles");

        var request = new PlatformBusinessRolesQuery(isBusiness: true);

        var result = await _mediator.Send(request, cancellationToken);

        return Success(result, "List of business roles retrieved successfully");
    }

    /// <summary>
    /// Suspend a business (Aegis Platform Admin only)
    /// </summary>
    [HttpPost("{businessId:guid}/suspend")]
    [RequireRole(RoleConstants.AegisAdmin)]
    public async Task<IActionResult> SuspendBusiness(Guid businessId, [FromBody] BusinessStatusReasonRequest? request = null, CancellationToken cancellationToken = default)
    {
        var command = new SuspendBusinessCommand
        {
            BusinessId = businessId,
            Reason = request?.Reason ?? "Suspended by platform administrator",
        };
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(Error(result.Message));
        return Success<object>(null!, result.Message);
    }

    /// <summary>
    /// Reactivate a suspended business (Aegis Platform Admin only)
    /// </summary>
    [HttpPost("{businessId:guid}/activate")]
    [RequireRole(RoleConstants.AegisAdmin)]
    public async Task<IActionResult> ActivateBusiness(Guid businessId, [FromBody] BusinessStatusReasonRequest? request = null, CancellationToken cancellationToken = default)
    {
        var command = new ReactivateBusinessCommand
        {
            BusinessId = businessId,
            Reason = request?.Reason ?? "Reactivated by platform administrator",
        };
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(Error(result.Message));
        return Success<object>(null!, result.Message);
    }

    // ── Dashboard Stats (/business/dashboard/stats) ───────────────────────────

    /// <summary>
    /// Returns aggregated platform statistics for the dashboard.
    /// Aegis admins see all-business stats; client admins see their own business.
    /// </summary>
    [HttpGet("dashboard/stats")]
    [MapToApiVersion("1.0")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetDashboardStatsQuery(), cancellationToken);
        return Success(result, "Dashboard statistics retrieved successfully");
    }

    // ── My Business (/business/me) ────────────────────────────────────────────

    /// <summary>
    /// Returns the profile of the currently authenticated user's business.
    /// </summary>
    [HttpGet("me")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.AegisAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetMyBusiness(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetBusinessByIdQuery(null), cancellationToken);
        if (result is null)
            return Error("Business not found", 404);

        var response = new BusinessProfileResponse
        {
            Id = result.Id,
            Name = result.Name,
            Description = result.Description,
            BusinessRegistrationNumber = result.BusinessRegistrationNumber,
            TaxIdentificationNumber = result.TIN,
            ContactEmail = result.ContactEmail,
            ContactPhone = result.ContactPhone,
            Industry = result.Industry,
            ServiceId = result.ServiceId ?? string.Empty,
            NRSBusinessId = result.FIRSBusinessId == Guid.Empty ? null : result.FIRSBusinessId.ToString(),
            IsActive = result.Status == AegisEInvoicing.Domain.Enums.BusinessStatus.Active,
            OnboardingCompleted = true,
            RegisteredAddress = result.RegisteredAddress is null ? null : new BusinessAddressResponse
            {
                Street = result.RegisteredAddress.Street ?? string.Empty,
                City = result.RegisteredAddress.City ?? string.Empty,
                State = result.RegisteredAddress.State ?? string.Empty,
                Country = result.RegisteredAddress.Country ?? string.Empty,
                PostalCode = result.RegisteredAddress.PostalCode ?? string.Empty
            }
        };
        return Success(response, "Business profile retrieved successfully");
    }

    /// <summary>
    /// Updates the profile of the currently authenticated user's business.
    /// </summary>
    [HttpPatch("me")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> UpdateMyBusiness([FromBody] UpdateBusinessRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.BusinessId.HasValue)
            return Error("User is not associated with a business", StatusCodes.Status403Forbidden);

        var address = Address.Create(
            request.RegisteredAddress.Street,
            request.RegisteredAddress.City,
            request.RegisteredAddress.State,
            request.RegisteredAddress.Country,
            request.RegisteredAddress.PostalCode);

        var command = new UpdateBusinessCommand(
            _currentUserService.BusinessId.Value,
            address,
            request.InvoicePrefix ?? string.Empty,
            request.Industry,
            request.ContactEmail,
            request.ContactPhone,
            request.Description,
            request.ServiceId,
            request.BusinessRegistrationNumber,
            request.TaxIdentificationNumber,
            !string.IsNullOrWhiteSpace(request.NRSBusinessId) && Guid.TryParse(request.NRSBusinessId, out var parsedId)
                ? parsedId
                : null);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return Error(result.Message);

        return Success<object>(null!, result.Message);
    }

}
