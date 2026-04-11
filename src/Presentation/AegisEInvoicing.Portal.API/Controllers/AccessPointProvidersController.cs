using Asp.Versioning;
using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.AccessPointProvider;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Create;
using AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Delete;
using AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Update;
using AegisEInvoicing.Application.Features.AccessPointProviders.Commands.SetBusinessAppProvider;
using AegisEInvoicing.Application.Features.AccessPointProviders.Commands.SetBusinessEnvironmentMode;
using AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;
using AegisEInvoicing.Application.Features.AccessPointProviders.Queries;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Manages Access Point Provider (APP) configurations and per-business provider/environment settings.
/// Provider CRUD is restricted to AegisAdmin; business-level switching is available to ClientAdmin.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[SwaggerTag("APP provider management — CRUD for provider configs and per-business provider/environment switching")]
[Authorize]
public class AccessPointProvidersController(
    ILogger<AccessPointProvidersController> logger) : BaseApiController
{
    // ─── Provider CRUD (AegisAdmin only) ─────────────────────────────────────

    [HttpGet]
    [SwaggerOperation(
        Summary = "List all APP provider configurations",
        OperationId = "GetAccessPointProviders",
        Tags = new[] { "Access Point Providers" })]
    [SwaggerResponse(200, "Paginated list of providers", typeof(ApiResponse<PaginatedList<AccessPointProvidersDto>>))]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetAccessPointProviders([FromQuery] GetAccessPointProvidersQuery request)
    {
        var result = await Mediator.Send(request);
        return Success(result, "Access point providers");
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new APP provider configuration",
        Description = "AegisAdmin only. Credentials are encrypted at rest.",
        OperationId = "CreateAccessPointProvider",
        Tags = new[] { "Access Point Providers" })]
    [SwaggerResponse(200, "Provider created", typeof(ApiResponse<CreateAccessPointProvidersResult>))]
    [SwaggerResponse(400, "Validation error or duplicate provider code")]
    [RequireAegisAdmin]
    public async Task<IActionResult> CreateAccessPointProvider([FromBody] AccessPointProviderRequest request)
    {
        var command = new CreateAccessPointProvidersCommand(
            request.Name,
            request.Description,
            request.Vendor,
            request.BaseUrl,
            request.CredentialsJson,
            request.SandboxBaseUrl,
            request.SandboxCredentialsJson);

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to create APP provider: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }

    [HttpPatch("{configurationId:guid}")]
    [SwaggerOperation(
        Summary = "Update an existing APP provider configuration",
        Description = "AegisAdmin only. Re-encrypts supplied credentials.",
        OperationId = "UpdateAccessPointProvider",
        Tags = new[] { "Access Point Providers" })]
    [SwaggerResponse(200, "Provider updated", typeof(ApiResponse<UpdateAccessPointProvidersResult>))]
    [SwaggerResponse(404, "Provider not found")]
    [RequireAegisAdmin]
    public async Task<IActionResult> UpdateAccessPointProvider(
        Guid configurationId,
        [FromBody] UpdateAccessPointProviderRequest request)
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
        {
            logger.LogWarning("Failed to update APP provider: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }

    [HttpDelete("{configurationId:guid}")]
    [SwaggerOperation(
        Summary = "Delete (soft) an APP provider configuration",
        OperationId = "DeleteAccessPointProvider",
        Tags = new[] { "Access Point Providers" })]
    [SwaggerResponse(200, "Provider deleted", typeof(ApiResponse<DeleteAccessPointProvidersResult>))]
    [SwaggerResponse(404, "Provider not found")]
    [RequireAegisAdmin]
    public async Task<IActionResult> DeleteAccessPointProvider(Guid configurationId)
    {
        var command = new DeleteAccessPointProvidersCommand(configurationId);
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to delete APP provider: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }

    // ─── Per-business settings (AegisAdmin or ClientAdmin) ───────────────────

    [HttpGet("businesses/{businessId:guid}/settings")]
    [SwaggerOperation(
        Summary = "Get current APP provider and environment mode for a business",
        Description = "Returns the active vendor and environment mode. ClientAdmin can only read their own business.",
        OperationId = "GetBusinessAppSettings",
        Tags = new[] { "Access Point Providers" })]
    [SwaggerResponse(200, "Settings retrieved", typeof(ApiResponse<BusinessAppSettingsDto>))]
    [SwaggerResponse(404, "Business not found or access denied")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetBusinessAppSettings(Guid businessId)
    {
        var result = await Mediator.Send(new GetBusinessAppSettingsQuery(businessId));

        if (result is null)
            return NotFound(Error("Business not found or access denied."));

        return Success(result, "Business APP settings");
    }

    [HttpPatch("businesses/{businessId:guid}/provider")]
    [SwaggerOperation(
        Summary = "Set active APP provider for a business",
        Description = "AegisAdmin can set for any business. ClientAdmin can set for their own business only. " +
                      "Pass null providerCode to reset to the platform default (Interswitch).",
        OperationId = "SetBusinessAppProvider",
        Tags = new[] { "Access Point Providers" })]
    [SwaggerResponse(200, "Provider updated", typeof(ApiResponse<SetBusinessAppProviderResult>))]
    [SwaggerResponse(400, "Unknown provider code or insufficient permissions")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientAdmin)]
    public async Task<IActionResult> SetBusinessAppProvider(
        Guid businessId,
        [FromBody] SetBusinessAppProviderRequest request)
    {
        var command = new SetBusinessAppProviderCommand(businessId, request.Vendor);
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to set APP provider for business {BusinessId}: {Message}", businessId, result.Message);
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }

    [HttpPatch("businesses/{businessId:guid}/environment")]
    [SwaggerOperation(
        Summary = "Switch sandbox / production environment for a business",
        Description = "AegisAdmin can change any business. ClientAdmin can change their own. " +
                      "Determines which credential set is used when calling the active APP provider.",
        OperationId = "SetBusinessEnvironmentMode",
        Tags = new[] { "Access Point Providers" })]
    [SwaggerResponse(200, "Environment mode updated", typeof(ApiResponse<SetBusinessEnvironmentModeResult>))]
    [SwaggerResponse(400, "Invalid request or insufficient permissions")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientAdmin)]
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
