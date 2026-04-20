using Asp.Versioning;
using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.AccessPointProvider;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Create;
using AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Delete;
using AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Update;
using AegisEInvoicing.Application.Features.AccessPointProviders.Commands.SetBusinessAppProvider;
using AegisEInvoicing.Application.Features.AccessPointProviders.Commands.SetBusinessEnvironmentMode;
using AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;
using AegisEInvoicing.Application.Features.AccessPointProviders.Queries;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Manages Access Point Provider (APP) configurations and per-business provider/environment settings.
/// Provider CRUD is restricted to AegisAdmin; business-level switching is available to ClientAdmin.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/access-point-providers")][Authorize]
public class AccessPointProvidersController(
    ILogger<AccessPointProvidersController> logger,
    IEnumerable<IAccessPointProviderClient> adapters) : BaseApiController
{
    // ─── Provider CRUD (AegisAdmin only) ─────────────────────────────────────

    [HttpGet("adapter-options")]    public IActionResult GetAdapterOptions()
    {
        var options = adapters.Select(a => new { adapterKey = a.ProviderCode, displayName = a.DisplayName });
        return Success(options, "Adapter options");
    }

    [HttpGet]    public async Task<IActionResult> GetAccessPointProviders([FromQuery] GetAccessPointProvidersQuery request)
    {
        var result = await Mediator.Send(request);
        return Success(result, "Access point providers");
    }

    [HttpGet("{id:guid}")]
    [RequireRole(RoleConstants.AegisAdmin)]
    public async Task<IActionResult> GetAccessPointProviderById(Guid id)
    {
        var result = await Mediator.Send(new GetAccessPointProviderByIdQuery(id));
        if (result is null)
            return Error("Access point provider not found", StatusCodes.Status404NotFound);
        return Success(result, "Access point provider retrieved");
    }

    [HttpPost]
    [RequireRole(RoleConstants.AegisAdmin)]
    public async Task<IActionResult> CreateAccessPointProvider([FromBody] AccessPointProviderRequest request)
    {
        var command = new CreateAccessPointProvidersCommand(
            request.Name,
            request.Description,
            request.AdapterKey,
            request.BaseUrl,
            request.CredentialsJson,
            request.SandboxBaseUrl,
            request.SandboxCredentialsJson);

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return Error(result.Message, StatusCodes.Status400BadRequest);

        return Created($"/api/v1/access-point-providers/{result.Id}", new { isSuccess = true, message = result.Message, id = result.Id });
    }

    [HttpPatch("{configurationId:guid}")]
    [RequireRole(RoleConstants.AegisAdmin)]
    public async Task<IActionResult> UpdateAccessPointProvider(Guid configurationId, [FromBody] UpdateAccessPointProviderRequest request)
    {
        var command = new UpdateAccessPointProvidersCommand(
            configurationId,
            request.Name,
            request.Description,
            request.BaseUrl,
            request.CredentialsJson,
            request.SandboxBaseUrl,
            request.SandboxCredentialsJson);

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return Error(result.Message, StatusCodes.Status400BadRequest);

        return Success(new { isSuccess = true, message = result.Message }, result.Message);
    }

    [HttpDelete("{configurationId:guid}")]
    [RequireRole(RoleConstants.AegisAdmin)]
    public async Task<IActionResult> DeleteAccessPointProvider(Guid configurationId)
    {
        var result = await Mediator.Send(new DeleteAccessPointProvidersCommand(configurationId));

        if (!result.IsSuccess)
            return Error(result.Message, StatusCodes.Status400BadRequest);

        return Success(new { isSuccess = true, message = result.Message }, result.Message);
    }

    [HttpGet("businesses/{businessId:guid}/settings")]    public async Task<IActionResult> GetBusinessAppSettings(Guid businessId)
    {
        var result = await Mediator.Send(new GetBusinessAppSettingsQuery(businessId));

        if (result is null)
            return NotFound(Error("Business not found or access denied."));

        return Success(result, "Business APP settings");
    }

    [HttpPatch("businesses/{businessId:guid}/provider")]
    [RequirePermission(PermissionConstants.ManageBusinessSettings)]
    public async Task<IActionResult> SetBusinessAppProvider(
        Guid businessId,
        [FromBody] SetBusinessAppProviderRequest request)
    {
        var command = new SetBusinessAppProviderCommand(businessId, request.AdapterKey);
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to set APP provider for business {BusinessId}: {Message}", businessId, result.Message);
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }

    [HttpPatch("businesses/{businessId:guid}/environment")]
    [RequirePermission(PermissionConstants.ManageBusinessSettings)]
    public async Task<IActionResult> SetBusinessEnvironmentMode(
        Guid businessId,
        [FromBody] SetBusinessEnvironmentModeRequest request)
    {
        var command = new SetBusinessEnvironmentModeCommand(businessId, request.EnvironmentMode);
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to set environment mode for business {BusinessId}: {Message}", businessId, result.Message);
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }
}
