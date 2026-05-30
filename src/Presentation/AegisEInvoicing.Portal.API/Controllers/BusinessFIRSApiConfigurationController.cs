using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.DTOs;
using AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.Queries.GetAllBusinessFIRSConfigurations;
using AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.Queries.GetBusinessFIRSConfiguration;
using AegisEInvoicing.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for managing Business FIRS API Configuration assignments
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/business-firs-configuration")]
[Authorize]public class BusinessFIRSApiConfigurationController : BaseApiController
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
    [HttpGet("current")]    [RequireRole(RoleConstants.ClientAdmin)]    public async Task<IActionResult> GetCurrentConfiguration(CancellationToken cancellationToken = default)
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
    [RequireRole(RoleConstants.AegisAdmin)]    public async Task<IActionResult> GetAllConfigurations(
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