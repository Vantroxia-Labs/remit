using AegisEInvoicing.Application.Features.VendorManagement.Commands.CreateInvoiceBroadcast;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.DeactivateBroadcast;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.ExtendBroadcastDueDate;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.MarkBroadcastSubmissions;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.RejectAllBroadcastInvoices;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.UpdateInvoiceBroadcast;
using AegisEInvoicing.Application.Features.VendorManagement.Queries.GetBroadcastById;
using AegisEInvoicing.Application.Features.VendorManagement.Queries.GetBroadcastList;
using AegisEInvoicing.Application.Features.VendorManagement.Queries.GetBroadcastSubmissions;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/invoice-broadcast")]
[Authorize]
public class InvoiceBroadcastController(IMediator mediator, ILogger<InvoiceBroadcastController> logger) : BaseApiController
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<InvoiceBroadcastController> _logger = logger;

    [HttpGet]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetListAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] BroadcastStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) return BadRequest(Error("Page number must be greater than 0"));
        if (pageSize < 1 || pageSize > 100) return BadRequest(Error("Page size must be between 1 and 100"));

        var result = await _mediator.Send(new GetBroadcastListQuery(pageNumber, pageSize, status), cancellationToken);
        return Paginated(result, $"Retrieved {result.TotalCount} broadcasts");
    }

    [HttpGet("{id:guid}")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetBroadcastByIdQuery(id), cancellationToken);
        if (result is null)
            return Error("Broadcast not found.", 404);
        return Success(result, "Broadcast retrieved successfully.");
    }

    [HttpPost]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateBroadcastRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateInvoiceBroadcastCommand(
            request.Title,
            request.InvoiceTypeCode,
            request.DueDate,
            request.RequiresApproval,
            request.Currency,
            request.Note,
            request.VendorIds,
            request.VendorGroupId,
            request.FrontendBaseUrl);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(Error(result.Message));
        return Created(new { result.Id, result.Message }, $"/api/v1/invoice-broadcast/{result.Id}");
    }

    [HttpPut("{id:guid}")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> UpdateAsync([FromRoute] Guid id, [FromBody] UpdateBroadcastRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new UpdateInvoiceBroadcastCommand(id, request.Title, request.Note), cancellationToken);
        if (!result.IsSuccess)
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Error(result.Message, 404)
                : BadRequest(Error(result.Message));
        return Success(new { result.Id, result.Message }, result.Message);
    }

    [HttpPatch("{id:guid}/extend-due-date")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> ExtendDueDateAsync([FromRoute] Guid id, [FromBody] ExtendDueDateRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ExtendBroadcastDueDateCommand(id, request.NewDueDate), cancellationToken);
        if (!result.IsSuccess)
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Error(result.Message, 404)
                : BadRequest(Error(result.Message));
        return Success(new { result.Id, result.Message }, result.Message);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> DeactivateAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DeactivateBroadcastCommand(id), cancellationToken);
        if (!result.IsSuccess)
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Error(result.Message, 404)
                : BadRequest(Error(result.Message));
        return Success(new { result.Message, result.HasPendingInvoices }, result.Message);
    }

    [HttpPost("{id:guid}/reject-all")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> RejectAllAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new RejectAllBroadcastInvoicesCommand(id), cancellationToken);
        if (!result.IsSuccess)
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Error(result.Message, 404)
                : BadRequest(Error(result.Message));
        return Success(new { result.Message }, result.Message);
    }

    // Stage 6 — Submission Management

    [HttpGet("{id:guid}/submissions")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetSubmissionsAsync(
        [FromRoute] Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetBroadcastSubmissionsQuery(id, pageNumber, pageSize), cancellationToken);
        return Paginated(result, $"Retrieved {result.TotalCount} submissions");
    }

    [HttpPatch("submissions/mark-paid")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> MarkPaidAsync([FromBody] BulkInvoiceIdsRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new MarkBroadcastSubmissionsPaidCommand(request.InvoiceIds), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(Error(result.Message));
        return Success(new { result.Message }, result.Message);
    }

    [HttpPatch("submissions/mark-rejected")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> MarkRejectedAsync([FromBody] BulkInvoiceIdsRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new MarkBroadcastSubmissionsRejectedCommand(request.InvoiceIds), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(Error(result.Message));
        return Success(new { result.Message }, result.Message);
    }
}

// ── Request Models ────────────────────────────────────────────────────────────

public record CreateBroadcastRequest(
    string Title,
    string InvoiceTypeCode,
    DateOnly DueDate,
    bool RequiresApproval,
    string Currency,
    string? Note,
    List<Guid>? VendorIds,
    Guid? VendorGroupId,
    string? FrontendBaseUrl);

public record UpdateBroadcastRequest(string Title, string? Note);
public record ExtendDueDateRequest(DateOnly NewDueDate);
public record BulkInvoiceIdsRequest(List<Guid> InvoiceIds);
