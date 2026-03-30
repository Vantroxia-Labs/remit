using EInvoiceIntegrator.Application.Common.Interfaces;
using EInvoiceIntegrator.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EInvoiceIntegrator.API.Controllers;

[ApiController]
[Route("api/v1/firs-api-configuration")]
[Authorize]
public class FIRSApiConfigurationController : ControllerBase
{
    private readonly IFIRSApiKeyService _firsApiKeyService;
    private readonly ILogger<FIRSApiConfigurationController> _logger;

    public FIRSApiConfigurationController(
        IFIRSApiKeyService firsApiKeyService,
        ILogger<FIRSApiConfigurationController> logger)
    {
        _firsApiKeyService = firsApiKeyService ?? throw new ArgumentNullException(nameof(firsApiKeyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the currently active FIRS API configuration
    /// </summary>
    [HttpGet("active")]
    [SwaggerOperation(
        Summary = "Get active FIRS API configuration",
        Description = "Retrieves the currently active FIRS API configuration being used for FIRS integration.",
        OperationId = "GetActiveConfiguration",
        Tags = new[] { "FIRS API Configuration" }
    )]
    [SwaggerResponse(200, "Active configuration retrieved successfully", typeof(FIRSApiConfigurationDto))]
    [SwaggerResponse(404, "No active FIRS API configuration found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<FIRSApiConfigurationDto>> GetActiveConfiguration()
    {
        try
        {
            var config = await _firsApiKeyService.GetActiveConfigurationAsync();
            if (config == null)
            {
                return NotFound("No active FIRS API configuration found");
            }

            return Ok(MapToDto(config));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active FIRS API configuration");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all FIRS API configurations (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [SwaggerOperation(
        Summary = "Get all FIRS API configurations",
        Description = "Retrieves all FIRS API configurations (both SaaS and On-Premise). Requires admin privileges.",
        OperationId = "GetAllConfigurations",
        Tags = new[] { "FIRS API Configuration" }
    )]
    [SwaggerResponse(200, "Configurations retrieved successfully", typeof(IEnumerable<FIRSApiConfigurationDto>))]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - Admin privileges required")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<IEnumerable<FIRSApiConfigurationDto>>> GetAllConfigurations()
    {
        try
        {
            var configurations = await _firsApiKeyService.GetAllConfigurationsAsync();
            return Ok(configurations.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving FIRS API configurations");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific FIRS API configuration by ID
    /// </summary>
    [HttpGet("{configurationId:guid}")]
    [SwaggerOperation(
        Summary = "Get FIRS API configuration by ID",
        Description = "Retrieves detailed information about a specific FIRS API configuration.",
        OperationId = "GetConfiguration",
        Tags = new[] { "FIRS API Configuration" }
    )]
    [SwaggerResponse(200, "Configuration retrieved successfully", typeof(FIRSApiConfigurationDto))]
    [SwaggerResponse(404, "Configuration not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<FIRSApiConfigurationDto>> GetConfiguration(Guid configurationId)
    {
        try
        {
            var config = await _firsApiKeyService.GetConfigurationAsync(configurationId);
            if (config == null)
            {
                return NotFound($"FIRS API configuration not found: {configurationId}");
            }

            return Ok(MapToDto(config));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving FIRS API configuration: {ConfigurationId}", configurationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a new SaaS FIRS API configuration (KMPG Admin only)
    /// </summary>
    [HttpPost("saas")]
    [Authorize(Policy = "KMPGAdminOnly")]
    [SwaggerOperation(
        Summary = "Create SaaS FIRS API configuration",
        Description = "Creates a new SaaS FIRS API configuration for multi-tenant cloud deployment. Requires KMPG admin privileges.",
        OperationId = "CreateSaaSConfiguration",
        Tags = new[] { "FIRS API Configuration" }
    )]
    [SwaggerResponse(201, "Configuration created successfully", typeof(FIRSApiConfigurationDto))]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - KMPG admin privileges required")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<FIRSApiConfigurationDto>> CreateSaaSConfiguration(
        [FromBody] CreateSaaSConfigurationRequest request)
    {
        try
        {
            var config = await _firsApiKeyService.CreateSaaSConfigurationAsync(
                request.Name,
                request.Description,
                request.Environment,
                request.BaseUrl,
                request.ApiKey,
                request.ApiSecret,
                request.DailyRequestLimit,
                request.MonthlyRequestLimit,
                request.ExpiresAt);

            return CreatedAtAction(
                nameof(GetConfiguration),
                new { configurationId = config.Id },
                MapToDto(config));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SaaS FIRS API configuration: {ConfigName}", request.Name);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a new On-Premise FIRS API configuration
    /// </summary>
    [HttpPost("on-premise")]
    [SwaggerOperation(
        Summary = "Create On-Premise FIRS API configuration",
        Description = "Creates a new On-Premise FIRS API configuration with domain restrictions. Requires KMPG approval before activation.",
        OperationId = "CreateOnPremiseConfiguration",
        Tags = new[] { "FIRS API Configuration" }
    )]
    [SwaggerResponse(201, "Configuration created successfully", typeof(FIRSApiConfigurationDto))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<FIRSApiConfigurationDto>> CreateOnPremiseConfiguration(
        [FromBody] CreateOnPremiseConfigurationRequest request)
    {
        try
        {
            var config = await _firsApiKeyService.CreateOnPremiseConfigurationAsync(
                request.Name,
                request.Description,
                request.Environment,
                request.BaseUrl,
                request.ApiKey,
                request.ApiSecret,
                request.AllowedDomains,
                request.DailyRequestLimit,
                request.MonthlyRequestLimit,
                request.ExpiresAt);

            return CreatedAtAction(
                nameof(GetConfiguration),
                new { configurationId = config.Id },
                MapToDto(config));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating On-Premise FIRS API configuration: {ConfigName}", request.Name);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Approves an On-Premise configuration (KMPG Admin only)
    /// </summary>
    [HttpPost("{configurationId:guid}/approve")]
    [Authorize(Policy = "KMPGAdminOnly")]
    [SwaggerOperation(
        Summary = "Approve On-Premise FIRS API configuration",
        Description = "Approves an On-Premise FIRS API configuration, allowing it to be used. Requires KMPG admin privileges.",
        OperationId = "ApproveOnPremiseConfiguration",
        Tags = new[] { "FIRS API Configuration" }
    )]
    [SwaggerResponse(200, "Configuration approved successfully")]
    [SwaggerResponse(404, "Configuration not found or cannot be approved")]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - KMPG admin privileges required")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult> ApproveOnPremiseConfiguration(
        Guid configurationId,
        [FromBody] ApproveConfigurationRequest request)
    {
        try
        {
            // TODO: Get current user ID from authentication context
            var currentUserId = Guid.CreateVersion7(); // Temporary - replace with actual user ID

            var result = await _firsApiKeyService.ApproveOnPremiseConfigurationAsync(
                configurationId,
                currentUserId,
                request.ApprovalNotes);

            if (!result)
            {
                return NotFound($"Configuration not found or cannot be approved: {configurationId}");
            }

            return Ok("Configuration approved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving FIRS API configuration: {ConfigurationId}", configurationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Revokes approval for a configuration (KMPG Admin only)
    /// </summary>
    [HttpPost("{configurationId:guid}/revoke")]
    [Authorize(Policy = "KMPGAdminOnly")]
    [SwaggerOperation(
        Summary = "Revoke configuration approval",
        Description = "Revokes approval for a FIRS API configuration, deactivating it. Requires KMPG admin privileges.",
        OperationId = "RevokeConfigurationApproval",
        Tags = new[] { "FIRS API Configuration" }
    )]
    [SwaggerResponse(200, "Configuration approval revoked successfully")]
    [SwaggerResponse(404, "Configuration not found")]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - KMPG admin privileges required")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult> RevokeConfigurationApproval(
        Guid configurationId,
        [FromBody] RevokeApprovalRequest request)
    {
        try
        {
            var result = await _firsApiKeyService.RevokeConfigurationApprovalAsync(
                configurationId,
                request.Reason);

            if (!result)
            {
                return NotFound($"Configuration not found: {configurationId}");
            }

            return Ok("Configuration approval revoked");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking FIRS API configuration approval: {ConfigurationId}", configurationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Sets a configuration as default
    /// </summary>
    [HttpPost("{configurationId:guid}/set-default")]
    [Authorize(Policy = "AdminOnly")]
    [SwaggerOperation(
        Summary = "Set configuration as default",
        Description = "Sets a FIRS API configuration as the default configuration for the deployment. Requires admin privileges.",
        OperationId = "SetDefaultConfiguration",
        Tags = new[] { "FIRS API Configuration" }
    )]
    [SwaggerResponse(200, "Configuration set as default successfully")]
    [SwaggerResponse(404, "Configuration not found or cannot be set as default")]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - Admin privileges required")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult> SetDefaultConfiguration(Guid configurationId)
    {
        try
        {
            var result = await _firsApiKeyService.SetDefaultConfigurationAsync(configurationId);
            if (!result)
            {
                return NotFound($"Configuration not found or cannot be set as default: {configurationId}");
            }

            return Ok("Configuration set as default");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default FIRS API configuration: {ConfigurationId}", configurationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets usage statistics for a configuration
    /// </summary>
    [HttpGet("{configurationId:guid}/usage")]
    [SwaggerOperation(
        Summary = "Get usage statistics for configuration",
        Description = "Retrieves usage statistics including daily/monthly request counts and limits for a FIRS API configuration.",
        OperationId = "GetUsageStats",
        Tags = new[] { "FIRS API Configuration" }
    )]
    [SwaggerResponse(200, "Usage statistics retrieved successfully", typeof(FIRSApiUsageStats))]
    [SwaggerResponse(404, "Configuration not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<FIRSApiUsageStats>> GetUsageStats(Guid configurationId)
    {
        try
        {
            var stats = await _firsApiKeyService.GetUsageStatsAsync(configurationId);
            if (stats == null)
            {
                return NotFound($"Configuration not found: {configurationId}");
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage stats for FIRS API configuration: {ConfigurationId}", configurationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates credentials for a configuration (Admin only)
    /// </summary>
    [HttpPut("{configurationId:guid}/credentials")]
    [Authorize(Policy = "AdminOnly")]
    [SwaggerOperation(
        Summary = "Update configuration credentials",
        Description = "Updates the API key and secret for a FIRS API configuration. Requires admin privileges.",
        OperationId = "UpdateCredentials",
        Tags = new[] { "FIRS API Configuration" }
    )]
    [SwaggerResponse(200, "Credentials updated successfully")]
    [SwaggerResponse(404, "Configuration not found")]
    [SwaggerResponse(401, "Unauthorized - Authentication required")]
    [SwaggerResponse(403, "Forbidden - Admin privileges required")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult> UpdateCredentials(
        Guid configurationId,
        [FromBody] UpdateCredentialsRequest request)
    {
        try
        {
            var result = await _firsApiKeyService.UpdateCredentialsAsync(
                configurationId,
                request.ApiKey,
                request.ApiSecret);

            if (!result)
            {
                return NotFound($"Configuration not found: {configurationId}");
            }

            return Ok("Credentials updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating credentials for FIRS API configuration: {ConfigurationId}", configurationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Validates subscription access (On-Premise deployments)
    /// </summary>
    [HttpGet("validate-subscription")]
    [SwaggerOperation(
        Summary = "Validate subscription access",
        Description = "Validates subscription access for On-Premise deployments, checking if the subscription key is valid and not expired.",
        OperationId = "ValidateSubscription",
        Tags = new[] { "FIRS API Configuration" }
    )]
    [SwaggerResponse(200, "Subscription validation result", typeof(SubscriptionValidationResult))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<SubscriptionValidationResult>> ValidateSubscription(Guid? businessId = null)
    {
        try
        {
            var isValid = await _firsApiKeyService.ValidateSubscriptionAsync(businessId);
            return Ok(new SubscriptionValidationResult(isValid, isValid ? "Valid subscription" : "Invalid or expired subscription"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating subscription for business: {BusinessId}", businessId);
            return StatusCode(500, "Internal server error");
        }
    }

    private static FIRSApiConfigurationDto MapToDto(FIRSApiConfiguration config)
    {
        return new FIRSApiConfigurationDto
        {
            Id = config.Id,
            Name = config.Name,
            Description = config.Description,
            DeploymentType = config.DeploymentType.ToString(),
            Environment = config.Environment,
            BaseUrl = config.BaseUrl,
            IsActive = config.IsActive,
            IsDefault = config.IsDefault,
            ExpiresAt = config.ExpiresAt,
            RequiresKMPGApproval = config.RequiresKMPGApproval,
            ApprovedBy = config.ApprovedBy,
            ApprovedAt = config.ApprovedAt,
            ApprovalNotes = config.ApprovalNotes,
            DailyRequestLimit = config.DailyRequestLimit,
            MonthlyRequestLimit = config.MonthlyRequestLimit,
            CurrentDailyUsage = config.CurrentDailyUsage,
            CurrentMonthlyUsage = config.CurrentMonthlyUsage,
            LastUsedAt = config.LastUsedAt,
            CreatedAt = config.CreatedAt,
            CreatedBy = config.CreatedBy
        };
    }
}

// DTOs
public record FIRSApiConfigurationDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string DeploymentType { get; init; } = default!;
    public string Environment { get; init; } = default!;
    public string BaseUrl { get; init; } = default!;
    public bool IsActive { get; init; }
    public bool IsDefault { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public bool RequiresKMPGApproval { get; init; }
    public Guid? ApprovedBy { get; init; }
    public DateTimeOffset? ApprovedAt { get; init; }
    public string? ApprovalNotes { get; init; }
    public int DailyRequestLimit { get; init; }
    public int MonthlyRequestLimit { get; init; }
    public int CurrentDailyUsage { get; init; }
    public int CurrentMonthlyUsage { get; init; }
    public DateTimeOffset LastUsedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
}

public record CreateSaaSConfigurationRequest
{
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Environment { get; init; } = default!;
    public string BaseUrl { get; init; } = default!;
    public string ApiKey { get; init; } = default!;
    public string ApiSecret { get; init; } = default!;
    public int DailyRequestLimit { get; init; } = 50000;
    public int MonthlyRequestLimit { get; init; } = 1500000;
    public DateTimeOffset? ExpiresAt { get; init; }
}

public record CreateOnPremiseConfigurationRequest
{
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Environment { get; init; } = default!;
    public string BaseUrl { get; init; } = default!;
    public string ApiKey { get; init; } = default!;
    public string ApiSecret { get; init; } = default!;
    public string AllowedDomains { get; init; } = default!;
    public int DailyRequestLimit { get; init; } = 10000;
    public int MonthlyRequestLimit { get; init; } = 300000;
    public DateTimeOffset? ExpiresAt { get; init; }
}

public record ApproveConfigurationRequest
{
    public string? ApprovalNotes { get; init; }
}

public record RevokeApprovalRequest
{
    public string Reason { get; init; } = default!;
}

public record UpdateCredentialsRequest
{
    public string ApiKey { get; init; } = default!;
    public string ApiSecret { get; init; } = default!;
}

public record SubscriptionValidationResult(bool IsValid, string Message);