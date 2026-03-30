using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.DTOs;
using AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.Queries.GetAllBusinessFIRSConfigurations;
using AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.Queries.GetBusinessFIRSConfiguration;
using AegisEInvoicing.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for managing Business FIRS API Configuration assignments
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/business-firs-configuration")]
[Authorize]
[SwaggerTag("Business FIRS API Configuration management - Assign, update, and manage FIRS API configurations for businesses")]
public class BusinessFIRSApiConfigurationController : BaseApiController
{
    private readonly ILogger<BusinessFIRSApiConfigurationController> _logger;

    public BusinessFIRSApiConfigurationController(ILogger<BusinessFIRSApiConfigurationController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get current business FIRS API configuration
    /// </summary>
    /// <returns>Business FIRS API configuration details</returns>
    [HttpGet("current")]
    [SwaggerOperation(
        Summary = "Get current business FIRS API configuration",
        Description = "Retrieves the current FIRS API configuration assigned to the authenticated business"
    )]
    [RequireRole(RoleConstants.ClientAdmin)]
    [SwaggerResponse(200, "Configuration retrieved successfully", typeof(ApiResponse<BusinessFIRSApiConfigurationDetailDto>))]
    [SwaggerResponse(404, "Configuration not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GetCurrentConfiguration(CancellationToken cancellationToken = default)
    {
        var query = new GetBusinessFIRSConfigurationQuery();
        var result = await Mediator.Send(query, cancellationToken);

        if (result is null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "No FIRS API configuration found for this business"
            });
        }
        return Success(result, "FIRS API configuration retrieved successfully");
    }

    /// <summary>
    /// Get all business FIRS API configurations (KMPG Admin only)
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="businessName">Filter by business name (optional)</param>
    /// <param name="configurationName">Filter by configuration name (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of business FIRS API configurations</returns>
    [HttpGet]
    [RequireRole(RoleConstants.AegisAdmin)]
    [SwaggerOperation(
        Summary = "Get all business FIRS API configurations (KMPG Admin only)",
        Description = "Retrieves a paginated list of all business FIRS API configurations with optional filtering"
    )]
    [SwaggerResponse(200, "Configurations retrieved successfully", typeof(ApiResponse<PaginatedList<BusinessFIRSApiConfigurationDto>>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - KMPG Admin role required", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GetAllConfigurations(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? businessName = null,
        [FromQuery] string? configurationName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAllBusinessFIRSConfigurationsQuery(pageNumber, pageSize, businessName, configurationName);
            var result = await Mediator.Send(query, cancellationToken);

            Response.Headers.Append("X-Pagination", System.Text.Json.JsonSerializer.Serialize(new
            {
                result.TotalCount,
                result.PageSize,
                result.PageNumber,
                result.TotalPages,
                result.HasPreviousPage,
                result.HasNextPage
            }));

            return Success(result, $"Retrieved {result.Items.Count} of {result.TotalCount} configurations");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business FIRS API configurations");
            return Error("An error occurred while retrieving configurations", StatusCodes.Status500InternalServerError);
        }
    }
}