using AegisEInvoicing.Application.Features.VatScheduleManagement.Commands.GenerateVatSchedule;
using AegisEInvoicing.Application.Features.VatScheduleManagement.Commands.MarkVatScheduleFiled;
using AegisEInvoicing.Application.Features.VatScheduleManagement.Queries.GetVatSchedules;
using AegisEInvoicing.Application.Features.VatScheduleManagement.Queries.GetVatScheduleWithItems;
using AegisEInvoicing.Application.Features.VatScheduleManagement.VatScheduleExport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/vat-schedule")]
    [Authorize]
    public class VatScheduleController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VatScheduleController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetSchedules([FromQuery] int? year)
        {
            var query = new GetVatSchedulesQuery { Year = year };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetScheduleById(Guid id)
        {
            var query = new GetVatScheduleWithItemsQuery(id);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateVatSchedule([FromBody] GenerateVatScheduleCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPatch("{id}/mark-filed")]
        public async Task<IActionResult> MarkAsFiled(Guid id)
        {
            var command = new MarkVatScheduleFiledCommand(id);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("{id}/export")]
        public async Task<IActionResult> ExportVatSchedule(Guid id)
        {
            var query = new ExportVatScheduleQuery(id);
            var fileBytes = await _mediator.Send(query);

            if (fileBytes == null || fileBytes.Length == 0)
            {
                return NotFound();
            }

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"VAT_Schedule_{id}.xlsx");
        }
    }
}
