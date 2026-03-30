using AegisEInvoicing.Application.Features.BusinessManagement.Commands.GenerateLicense;
using AegisEInvoicing.Application.Features.LicenseManagement.Commands.ActivateLicense;
using AegisEInvoicing.Application.Features.LicenseManagement.Queries.GetAllLicenses;
using AegisEInvoicing.Application.Features.LicenseManagement.Queries.GetLicenseHistory;
using Asp.Versioning;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// License management endpoints (Aegis Admin only)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class LicenseController(
    IMediator mediator,
    ILogger<LicenseController> logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<LicenseController> _logger = logger;

    /// <summary>
    /// Generate a license key for an on-premise business
    /// </summary>
    /// <param name="request">License generation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated license key</returns>
    /// <response code="200">License generated successfully</response>
    /// <response code="400">Invalid request or business not configured for on-premise</response>
    /// <response code="404">Business not found</response>
    /// <response code="403">User does not have AegisAdmin role</response>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateLicenseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Roles = RoleConstants.AegisAdmin)]
    public async Task<IActionResult> GenerateLicense(
        [FromBody] GenerateLicenseRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "License generation requested for business {BusinessId}",
            request.BusinessId);

        var command = new GenerateLicenseCommand
        {
            BusinessId = request.BusinessId,
            ExpiryDate = request.ExpiryDate
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Activate a license key for the current business
    /// Client Admin only - activates license received from Aegis
    /// </summary>
    /// <param name="request">License activation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Activation result with license details</returns>
    /// <response code="200">License activated successfully</response>
    /// <response code="400">Invalid license key or business not OnPremise</response>
    /// <response code="403">License key not valid for this business</response>
    /// <response code="404">Business not found</response>
    [HttpPost("activate")]
    [Authorize(Roles = RoleConstants.ClientAdmin)]
    [ProducesResponseType(typeof(ActivateLicenseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateLicense(
        [FromBody] ActivateLicenseRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "License activation requested by user for their business");

        var command = new ActivateLicenseCommand(request.LicenseKey);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all OnPremise business licenses with pagination and filtering
    /// Aegis Admin only - view all licenses across all businesses
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="status">Filter by status: Active, Expired, ExpiringSoon, NotActivated, or null for all</param>
    /// <param name="searchTerm">Search by business name or license key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of licenses</returns>
    /// <response code="200">Licenses retrieved successfully</response>
    /// <response code="403">User does not have AegisAdmin role</response>
    [HttpGet("all")]
    [Authorize(Roles = RoleConstants.AegisAdmin)]
    [ProducesResponseType(typeof(GetAllLicensesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllLicenses(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        if (pageSize > 100)
        {
            return BadRequest("Page size cannot exceed 100");
        }

        _logger.LogInformation(
            "Aegis Admin fetching all licenses - Page: {PageNumber}, Size: {PageSize}, Status: {Status}",
            pageNumber, pageSize, status ?? "All");

        var query = new GetAllLicensesQuery(pageNumber, pageSize, status, searchTerm);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get license information for the current business with pagination and filtering
    /// Client Admin only - view their own license details
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="status">Filter by status: Active, Expired, ExpiringSoon, NotActivated</param>
    /// <param name="searchTerm">Search in license key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current license information with pagination</returns>
    /// <response code="200">License information retrieved successfully</response>
    /// <response code="404">Business not found</response>
    /// <response code="403">User does not have ClientAdmin role</response>
    [HttpGet("history")]
    [Authorize(Roles = RoleConstants.ClientAdmin)]
    [ProducesResponseType(typeof(GetLicenseHistoryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLicenseHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        if (pageSize > 100)
        {
            return BadRequest("Page size cannot exceed 100");
        }

        _logger.LogInformation(
            "Client admin requesting license history - Page: {PageNumber}, Size: {PageSize}, Status: {Status}, Search: {SearchTerm}",
            pageNumber, pageSize, status ?? "All", searchTerm ?? "None");

        var query = new GetLicenseHistoryQuery(pageNumber, pageSize, status, searchTerm);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }
}

/// <summary>
/// Request model for activating a license
/// </summary>
public class ActivateLicenseRequest
{
    /// <summary>
    /// License key received from Aegis
    /// </summary>
    public string LicenseKey { get; set; } = null!;
}

/// <summary>
/// Request model for generating a license
/// </summary>
public class GenerateLicenseRequest
{
    /// <summary>
    /// Business ID to generate license for
    /// </summary>
    public Guid BusinessId { get; set; }

    /// <summary>
    /// License expiry date (with time)
    /// Must be in the future and not more than 10 years from now
    /// </summary>
    public DateTime ExpiryDate { get; set; }
}

