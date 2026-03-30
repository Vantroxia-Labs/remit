using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.Miscellenous.DTOs;
using AegisEInvoicing.Application.Features.Miscellenous.Enums;
using AegisEInvoicing.Application.Features.Miscellenous.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for Miscellenous Records        
/// </summary>
/// [ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[SwaggerTag("Business Operations which includes onboarding, updating, fetching business")]
public class MiscellenousController(
    IMediator mediator,
    ILogger<MiscellenousController> logger,
    IIntegrationService integrationService) : BaseApiController
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<MiscellenousController> _logger = logger;
    private readonly IIntegrationService _integrationService = integrationService;

    [HttpGet("states")]
    [SwaggerOperation(Description = "This endpoint allows fetching list of states that are available in Nigeria.")]
    [SwaggerResponse(200, "Request successful", typeof(ApiResponse<PaginatedList<StatesSummaryDto>>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<PaginatedList<StatesSummaryDto>>))]
    public async Task<IActionResult> GetStates(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetch list of states");

        var request = new StatesQuery();

        var result = await _mediator.Send(request, cancellationToken);

        return Success(result, "List of states");
    }

    [HttpGet("cities/{stateName}")]
    [SwaggerOperation(Description = "This endpoint allows fetching list of cisites tied to a given state in Nigeria.")]
    [SwaggerResponse(200, "Request successful", typeof(ApiResponse<PaginatedList<CitiesSummaryDto>>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<PaginatedList<CitiesSummaryDto>>))]
    public async Task<IActionResult> GetCities(string stateName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetch list of cities");

        var request = new CitiesQuery(stateName.Trim());

        var result = await _mediator.Send(request, cancellationToken);

        return Success(result, "List of cities");
    }

    [HttpGet("industry")]
    [SwaggerOperation(Description = "This endpoint allows fetching list of industries")]
    [ProducesResponseType(typeof(ApiResponse<List<IndustryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public IActionResult GetIndustry(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching list of industries");

        var industries = Enum.GetValues(typeof(Industry))
            .Cast<Industry>()
            .Select(i => new IndustryDto { Name = i.ToString() })
            .ToList();

        return Success(industries, "List of industries retrieved successfully");
    }

    [HttpGet("regions")]
    [SwaggerOperation(Description = "This endpoint allows fetching list of Nigerian geo-political regions")]
    [ProducesResponseType(typeof(ApiResponse<List<RegionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRegions(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching list of Nigerian regions");

        var request = new GetRegionsQuery();

        var result = await _mediator.Send(request, cancellationToken);

        return Success(result, "List of Nigerian regions retrieved successfully");
    }

    
}
