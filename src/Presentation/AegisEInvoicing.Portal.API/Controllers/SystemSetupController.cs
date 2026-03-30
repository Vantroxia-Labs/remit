using Asp.Versioning;
using EInvoiceIntegrator.API.Models.SystemSetup;
using EInvoiceIntegrator.Application.Features.SubscriptionKeys.Queries;
using EInvoiceIntegrator.Application.Features.System.Commands;
using EInvoiceIntegrator.Application.Features.System.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using SystemSetupResult = EInvoiceIntegrator.Application.Features.System.Commands.SystemSetupResult;
using SystemSetupStatusDto = EInvoiceIntegrator.Application.Features.System.Queries.SystemSetupStatusDto;

namespace EInvoiceIntegrator.API.Controllers;

/// <summary>
/// System setup and initialization controller - Used for first-time system configuration
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/setup")]
public class SystemSetupController(
    IMediator mediator,
    ILogger<SystemSetupController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<SystemSetupController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Check if system setup is required
    /// </summary>
    [HttpGet("status")]
    [SwaggerOperation(
        Summary = "Check system setup status",
        Description = "Retrieves the current system setup status to determine if initial configuration is required or if the system is already configured.",
        OperationId = "GetSetupStatus",
        Tags = new[] { "System Setup" }
    )]
    [SwaggerResponse(200, "System setup status retrieved successfully", typeof(SystemSetupStatusDto))]
    [SwaggerResponse(500, "Internal server error occurred while checking setup status")]
    public async Task<ActionResult<SystemSetupStatusDto>> GetSetupStatus()
    {
        try
        {
            var query = new GetSystemSetupStatusQuery();
            var status = await _mediator.Send(query);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking system setup status");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Initialize system as SaaS deployment
    /// </summary>
    [HttpPost("initialize/saas")]
    [SwaggerOperation(
        Summary = "Initialize system as SaaS deployment",
        Description = "Performs initial system setup for SaaS (Software as a Service) deployment mode. Creates the primary organization, configures multi-tenancy settings, and sets up the first administrator account.",
        OperationId = "InitializeSaaS",
        Tags = new[] { "System Setup" }
    )]
    [SwaggerResponse(200, "SaaS system initialized successfully", typeof(SystemSetupResult))]
    [SwaggerResponse(400, "Invalid request data or system already initialized")]
    [SwaggerResponse(500, "Internal server error occurred during SaaS initialization")]
    public async Task<ActionResult<SystemSetupResult>> InitializeSaaS([FromBody] InitializeSaaSRequest request)
    {
        try
        {
            var command = new InitializeSaaSSystemCommand
            {
                OrganizationName = request.OrganizationName,
                AdminFirstName = request.AdminFirstName,
                AdminLastName = request.AdminLastName,
                AdminEmail = request.AdminEmail,
                AdminPassword = request.AdminPassword,
                AllowSelfOnboarding = request.AllowSelfOnboarding,
                MaxBusinessesAllowed = request.MaxBusinessesAllowed
            };

            var result = await _mediator.Send(command);
            
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing SaaS system");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Initialize system as On-Premise deployment with subscription key validation
    /// </summary>
    [HttpPost("initialize/on-premise")]
    [SwaggerOperation(
        Summary = "Initialize system as On-Premise deployment",
        Description = "Performs initial system setup for On-Premise deployment mode. Validates the provided subscription key, creates the organization based on subscription data, and sets up the first administrator account.",
        OperationId = "InitializeOnPremise",
        Tags = new[] { "System Setup" }
    )]
    [SwaggerResponse(200, "On-Premise system initialized successfully", typeof(SystemSetupResult))]
    [SwaggerResponse(400, "Invalid subscription key or request data")]
    [SwaggerResponse(500, "Internal server error occurred during On-Premise initialization")]
    public async Task<ActionResult<SystemSetupResult>> InitializeOnPremise([FromBody] InitializeOnPremiseRequest request)
    {
        try
        {
            // First, validate the subscription key
            var validationQuery = new ValidateSubscriptionKeyQuery { Key = request.SubscriptionKey };
            var validationResult = await _mediator.Send(validationQuery);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid subscription key provided for on-premise setup: {Error}", validationResult.ValidationError);
                return BadRequest(new { 
                    error = "Invalid subscription key", 
                    message = validationResult.ValidationError 
                });
            }

            // Use the validated subscription key data for setup
            var command = new InitializeOnPremiseSystemCommand
            {
                OrganizationName = validationResult.BusinessName!, // Use business name from subscription key
                LicenseKey = request.SubscriptionKey, // Use subscription key as license key
                ContactEmail = validationResult.ContactEmail!,
                ContactPhone = request.ContactPhone ?? string.Empty,
                AdminFirstName = request.AdminFirstName,
                AdminLastName = request.AdminLastName,
                AdminEmail = request.AdminEmail,
                AdminPassword = request.AdminPassword,
                SubscriptionKeyId = validationResult.SubscriptionKeyId!.Value
            };

            var result = await _mediator.Send(command);
            
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            _logger.LogInformation("On-Premise system initialized successfully with subscription key: {SubscriptionKey}", request.SubscriptionKey);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing On-Premise system");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update On-Premise license (KMPG admin only)
    /// </summary>
    [HttpPut("license")]
    [SwaggerOperation(
        Summary = "Update On-Premise license key",
        Description = "Updates the license key and expiry date for an On-Premise deployment. This endpoint is restricted to KMPG administrators only and is used to renew or modify existing licenses.",
        OperationId = "UpdateLicense",
        Tags = new[] { "System Setup" }
    )]
    [SwaggerResponse(200, "License updated successfully")]
    [SwaggerResponse(400, "Invalid license key or request data")]
    [SwaggerResponse(401, "Authentication required")]
    [SwaggerResponse(403, "Access forbidden - KMPG admin privileges required")]
    [SwaggerResponse(500, "Internal server error occurred during license update")]
    public async Task<ActionResult> UpdateLicense([FromBody] UpdateLicenseRequest request)
    {
        try
        {
            var command = new UpdateLicenseCommand
            {
                LicenseKey = request.LicenseKey,
                ExpiryDate = request.ExpiryDate
            };

            var result = await _mediator.Send(command);
            
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            return Ok(result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating license");
            return StatusCode(500, "Internal server error");
        }
    }
}