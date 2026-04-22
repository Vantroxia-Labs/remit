using AegisEInvoicing.Application.Features.WhtScheduleManagement.Commands.GenerateWhtSchedule;
using AegisEInvoicing.Application.Features.WhtScheduleManagement.Commands.MarkWhtScheduleFiled;
using AegisEInvoicing.Application.Features.WhtScheduleManagement.Queries.GetWhtSchedules;
using AegisEInvoicing.Application.Features.WhtScheduleManagement.Queries.GetWhtScheduleWithItems;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/wht-schedule")]
[Authorize]
public class WhtScheduleController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>Get all WHT schedules for the current business, optionally filtered by year.</summary>
    [HttpGet]
    public async Task<IActionResult> GetSchedules([FromQuery] int? year, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWhtSchedulesQuery(year), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get a single WHT schedule with all its vendor line items.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetScheduleById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWhtScheduleWithItemsQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Generate a new WHT schedule for the given year and month.</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateWhtSchedule(
        [FromBody] GenerateWhtScheduleCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Mark a WHT schedule as filed with the tax authority.</summary>
    [HttpPatch("{id:guid}/mark-filed")]
    public async Task<IActionResult> MarkAsFiled(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkWhtScheduleFiledCommand(id), cancellationToken);
        return Ok(result);
    }
}
