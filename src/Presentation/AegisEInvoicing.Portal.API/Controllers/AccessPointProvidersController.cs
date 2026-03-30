using Asp.Versioning;
using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.AccessPointProvider;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Create;
using AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Delete;
using AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Update;
using AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;
using AegisEInvoicing.Application.Features.AccessPointProviders.Queries;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for Aegis user management operations
/// All operations enforce platform admin access only - Aegis users are not tied to any business
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[SwaggerTag("Includes operation for creating, updating, fetching list of access point provides")]
[Authorize]
public class AccessPointProvidersController(IMediator mediator, ILogger<AccessPointProvidersController> logger) : BaseApiController
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<AccessPointProvidersController> _logger = logger;

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get list of access point providers",
        Description = "Retrieves a paginated list of all access point providers. Available to Aegis Admins and Client Admins.",
        OperationId = "GetAccessPointProvider",
        Tags = new[] { "Access Point Providers" }
    )]
    [SwaggerResponse(200, "Return a list of all access providers", typeof(ApiResponse<PaginatedList<AccessPointProvidersDto>>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<PaginatedList<AccessPointProvidersDto>>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<PaginatedList<AccessPointProvidersDto>>))]
    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<PaginatedList<AccessPointProvidersDto>>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetAccessPointProvider([FromQuery] GetAccessPointProvidersQuery request)
    {
        _logger.LogInformation("About fetching list of access point providers");

        var result = await Mediator.Send(request);

        return Success(result, "List of access providers");
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new access point provider",
        Description = "Creates a new access point provider configuration with the specified details. Only Aegis Admins can perform this operation.",
        OperationId = "CreateAccessPointProvider",
        Tags = new[] { "Access Point Providers" }
    )]
    [SwaggerResponse(200, "Access point provider created successfully", typeof(ApiResponse<CreateAccessPointProvidersResult>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [RequireAegisAdmin]
    public async Task<IActionResult> CreateAccessPointProvider([FromBody] AccessPointProviderRequest request)
    {
        _logger.LogInformation("KMPG platform admin about creating access point providers");

        var command = new CreateAccessPointProvidersCommand(request.Name, request.Description, request.Environment, request.BaseUrl,request.ApiKey, request.ApiSecret);

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to create access point providers: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }

    [HttpPatch("{configurationId}")]
    [SwaggerOperation(
        Summary = "Update an existing access point provider",
        Description = "Updates an access point provider configuration identified by the configuration ID. Only Aegis Admins can perform this operation.",
        OperationId = "UpdateAccessPointProvider",
        Tags = new[] { "Access Point Providers" }
    )]
    [SwaggerResponse(200, "Access point provider updated successfully", typeof(ApiResponse<UpdateAccessPointProvidersResult>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Access point provider not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [RequireAegisAdmin]
    public async Task<IActionResult> UpdateAccessPointProvider(Guid configurationId, [FromBody] UpdateAccessPointProviderRequest request)
    {
        _logger.LogInformation("KMPG platform admin about updating access point providers");

        var command = new UpdateAccessPointProvidersCommand(configurationId, request.Name, request.Description, request.Environment, request.BaseUrl, request.ApiKey, request.ApiSecret);

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to update access point providers: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }

    [HttpDelete("{configurationId}")]
    [SwaggerOperation(
        Summary = "Delete an access point provider",
        Description = "Deletes an access point provider configuration identified by the configuration ID. Only Aegis Admins can perform this operation.",
        OperationId = "DeleteAccessPointProvider",
        Tags = new[] { "Access Point Providers" }
    )]
    [SwaggerResponse(200, "Access point provider deleted successfully", typeof(ApiResponse<DeleteAccessPointProvidersResult>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Access point provider not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [RequireAegisAdmin]
    public async Task<IActionResult> DeleteAccessPointProvider(Guid configurationId)
    {
        _logger.LogInformation("KMPG platform admin about deleting access point providers");

        var command = new DeleteAccessPointProvidersCommand(configurationId);

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to delete access point providers: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }
}
