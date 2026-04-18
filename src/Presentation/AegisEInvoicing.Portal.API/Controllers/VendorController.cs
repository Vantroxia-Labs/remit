using AegisEInvoicing.Application.Features.VendorManagement.Commands.CreateVendor;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.CreateVendorGroup;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.DeleteVendor;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.DeleteVendorGroup;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.ToggleVendorStatus;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.UpdateVendor;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.UpdateVendorGroup;
using AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorById;
using AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorGroupById;
using AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorGroupList;
using AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorList;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/vendor")]
[Authorize]
public class VendorController(IMediator mediator, ILogger<VendorController> logger) : BaseApiController
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<VendorController> _logger = logger;

    // ── Vendor Group Endpoints ────────────────────────────────────────────────

    [HttpGet("groups")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetGroupListAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) return BadRequest(Error("Page number must be greater than 0"));
        if (pageSize < 1 || pageSize > 100) return BadRequest(Error("Page size must be between 1 and 100"));

        var result = await _mediator.Send(new GetVendorGroupListQuery(pageNumber, pageSize, searchTerm), cancellationToken);
        return Paginated(result, $"Retrieved {result.TotalCount} vendor groups");
    }

    [HttpGet("groups/{id:guid}")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetGroupByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetVendorGroupByIdQuery(id), cancellationToken);
        if (result is null)
            return Error("Vendor group not found.", 404);
        return Success(result, "Vendor group retrieved successfully.");
    }

    [HttpPost("groups")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> CreateGroupAsync([FromBody] CreateVendorGroupRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateVendorGroupCommand(request.Name, request.Description);
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(Error(result.Message));
        return Created(new { result.Id, result.Message }, $"/api/v1/vendor/groups/{result.Id}");
    }

    [HttpPut("groups/{id:guid}")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> UpdateGroupAsync([FromRoute] Guid id, [FromBody] UpdateVendorGroupRequest request, CancellationToken cancellationToken = default)
    {
        var command = new UpdateVendorGroupCommand(id, request.Name, request.Description);
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Error(result.Message, 404)
                : BadRequest(Error(result.Message));
        return Success(new { result.Id, result.Message }, result.Message);
    }

    [HttpDelete("groups/{id:guid}")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> DeleteGroupAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DeleteVendorGroupCommand(id), cancellationToken);
        if (!result.IsSuccess)
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Error(result.Message, 404)
                : BadRequest(Error(result.Message));
        return Success(new { result.Id, result.Message }, result.Message);
    }

    // ── Vendor Endpoints ──────────────────────────────────────────────────────

    [HttpGet]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetVendorListAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? vendorGroupId = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) return BadRequest(Error("Page number must be greater than 0"));
        if (pageSize < 1 || pageSize > 100) return BadRequest(Error("Page size must be between 1 and 100"));

        var result = await _mediator.Send(new GetVendorListQuery(pageNumber, pageSize, vendorGroupId, searchTerm), cancellationToken);
        return Paginated(result, $"Retrieved {result.TotalCount} vendors");
    }

    [HttpGet("{id:guid}")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetVendorByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetVendorByIdQuery(id), cancellationToken);
        if (result is null)
            return Error("Vendor not found.", 404);
        return Success(result, "Vendor retrieved successfully.");
    }

    [HttpPost]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> CreateVendorAsync([FromBody] CreateVendorRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateVendorCommand(request.BusinessName, request.Email, request.Phone, request.VendorGroupId);
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(Error(result.Message));
        return Created(new { result.Id, result.Message }, $"/api/v1/vendor/{result.Id}");
    }

    [HttpPut("{id:guid}")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> UpdateVendorAsync([FromRoute] Guid id, [FromBody] UpdateVendorRequest request, CancellationToken cancellationToken = default)
    {
        var command = new UpdateVendorCommand(id, request.BusinessName, request.Email, request.Phone, request.VendorGroupId);
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Error(result.Message, 404)
                : BadRequest(Error(result.Message));
        return Success(new { result.Id, result.Message }, result.Message);
    }

    [HttpDelete("{id:guid}")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> DeleteVendorAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DeleteVendorCommand(id), cancellationToken);
        if (!result.IsSuccess)
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Error(result.Message, 404)
                : BadRequest(Error(result.Message));
        return Success(new { result.Id, result.Message }, result.Message);
    }

    [HttpPatch("{id:guid}/toggle-status")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> ToggleStatusAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ToggleVendorStatusCommand(id), cancellationToken);
        if (!result.IsSuccess)
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Error(result.Message, 404)
                : BadRequest(Error(result.Message));
        return Success(new { result.Id, result.Message }, result.Message);
    }
}

// ── Request Models ────────────────────────────────────────────────────────────

public record CreateVendorGroupRequest(string Name, string? Description);
public record UpdateVendorGroupRequest(string Name, string? Description);
public record CreateVendorRequest(string BusinessName, string Email, string? Phone, Guid VendorGroupId);
public record UpdateVendorRequest(string BusinessName, string Email, string? Phone, Guid VendorGroupId);
