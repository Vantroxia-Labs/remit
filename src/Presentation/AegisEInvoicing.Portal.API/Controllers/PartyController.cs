using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.Party.Request;
using AegisEInvoicing.Portal.API.Models.Party.Response;
using AegisEInvoicing.Application.Features.PartyManagement.Commands.CreateBulkParty;
using AegisEInvoicing.Application.Features.PartyManagement.Commands.CreateParty;
using AegisEInvoicing.Application.Features.PartyManagement.Commands.DeactivateParty;
using AegisEInvoicing.Application.Features.PartyManagement.Commands.UpdateParty;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using AegisEInvoicing.Application.Features.PartyManagement.Queries.GetPartiesByBusinessId;
using AegisEInvoicing.Application.Features.PartyManagement.Queries.GetPartyById;
using AegisEInvoicing.Application.Features.PartyManagement.Queries.GetPartyList;
using AegisEInvoicing.Application.Features.PartyManagement.Queries.ValidateParty;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for party management operations
/// Handles CRUD operations for parties (customers, suppliers, etc.) within business context
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class PartyController(IMediator mediator, ILogger<PartyController> logger) : BaseApiController
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<PartyController> _logger = logger;

    /// <summary>
    /// Create a new party
    /// </summary>
    /// <param name="request">Party creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created party information</returns>
    [HttpPost]
    [Authorize(Policy = "RequireSaasSubscription")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> CreateAsync([FromBody] CreatePartyRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating party with name: {Name}, TIN: {TIN}", request.Name, request.TaxIdentificationNumber);

        var addressDto = new CreateAddressDto(
            request.Address.Street,
            request.Address.City,
            request.Address.State,
            request.Address.Country,
            request.Address.PostalCode,
            request.Address.Lga);

        var command = new CreatePartyCommand(
            request.Name,
            request.Phone,
            request.Email,
            request.TaxIdentificationNumber,
            addressDto,
            request.Description);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to create party: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        _logger.LogInformation("Party created successfully. ID: {PartyId}", result.PartyId);

        var response = new CreatePartyResponse
        {
            PartyId = result.PartyId ?? Guid.Empty, // Use Empty GUID if PartyId is null (shouldn't happen for successful create)
            Message = result.Message
        };

        return Created($"/api/v1/party/{result.PartyId}", Success(response, "Party created successfully"));
    }

    /// <summary>
    /// This endpoint allows business admin to upload an excel to create multiple bulk party
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("CreateBulkParty")]
    [Authorize(Policy = "RequireSaasSubscription")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> CreateBulkParty(CreateBulkPartyUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Client admin can call this endpoint to create a list of bulk party");

        var command = new CreateBulkPartyCommand(request.file);

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to bulk muliple bulk party withh: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }

    /// <summary>
    /// Get party by ID
    /// </summary>
    /// <param name="id">Party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Party details</returns>
    [HttpGet("{id:guid}")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving party with ID: {Id}", id);

        var query = new GetPartyByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success || result.Party == null)
        {
            _logger.LogWarning("Party not found with ID: {Id}", id);
            return Error(result.Message, 404);
        }

        var response = new PartyResponse
        {
            Id = result.Party.Id,
            Name = result.Party.Name,
            Phone = result.Party.Phone,
            Email = result.Party.Email,
            TaxIdentificationNumber = result.Party.TaxIdentificationNumber,
            Address = new AddressResponse
            {
                Street = result.Party.Address.Street,
                City = result.Party.Address.City,
                State = result.Party.Address.State,
                Country = result.Party.Address.Country,
                PostalCode = result.Party.Address.PostalCode,
                Lga = result.Party.Address.Lga
            },
            CreatedAt = result.Party.CreatedAt,
            UpdatedAt = result.Party.UpdatedAt,
            CreatedBy = result.Party.CreatedBy,
            UpdatedBy = result.Party.UpdatedBy
        };

        return Success(response, "Party retrieved successfully");
    }

    /// <summary>
    /// Update an existing party
    /// </summary>
    /// <param name="id">Party ID</param>
    /// <param name="request">Updated party details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireSaasSubscription")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> UpdateAsync([FromRoute] Guid id, [FromBody] UpdatePartyRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating party with ID: {Id}", id);

        var addressDto = new UpdateAddressDto(
            request.Address.Street,
            request.Address.City,
            request.Address.State,
            request.Address.Country,
            request.Address.PostalCode,
            request.Address.Lga);

        var command = new UpdatePartyCommand(
            id,
            request.Name,
            request.Phone,
            request.Email,
            request.TaxIdentificationNumber,
            addressDto);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to update party: {Message}", result.Message);
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Error(result.Message, 404)
                : BadRequest(Error(result.Message));
        }

        _logger.LogInformation("Party updated successfully. ID: {PartyId}", result.PartyId);

        var response = new UpdatePartyResponse
        {
            PartyId = result.PartyId ?? id, // Use the original ID if PartyId is null
            Message = result.Message
        };

        return Success(response, "Party updated successfully");
    }

    /// <summary>
    /// Validate party fields for existence
    /// </summary>
    /// <param name="request">Validation request containing field types and values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation results indicating whether each field exists</returns>
    [HttpPost("validate")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> ValidatePartyFieldsAsync(
        [FromBody] PartyValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Requested party field validation for {FieldCount} fields",
            request.ValidationFields?.Count ?? 0);

        if (request.ValidationFields == null || !request.ValidationFields.Any())
        {
            return BadRequest(Error("At least one validation field is required"));
        }

        var query = new ValidatePartyQuery(request.ValidationFields);
        var validationResults = await _mediator.Send(query, cancellationToken);

        var existingFields = validationResults.Where(r => r.Value).Select(r => r.Key).ToList();
        var nonExistingFields = validationResults.Where(r => !r.Value).Select(r => r.Key).ToList();

        var message = "Validation completed";
        if (existingFields.Any())
        {
            message += $". Found existing: {string.Join(", ", existingFields)}";
        }
        if (nonExistingFields.Any())
        {
            message += $". Not found: {string.Join(", ", nonExistingFields)}";
        }

        var response = new PartyValidationResponse
        {
            ValidationResults = validationResults,
            Message = message
        };

        _logger.LogInformation("party field validation completed. Existing fields: {ExistingCount}, Non-existing fields: {NonExistingCount}",
            existingFields.Count, nonExistingFields.Count);

        return Success(response, message);
    }

    /// <summary>
    /// Deactivate a party (soft delete)
    /// </summary>
    /// <param name="id">Party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deactivation result</returns>
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "RequireSaasSubscription")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> DeactivateAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating party with ID: {Id}", id);

        var command = new DeactivatePartyCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to deactivate party: {Message}", result.Message);
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Error(result.Message, 404)
                : BadRequest(Error(result.Message));
        }

        _logger.LogInformation("Party deactivated successfully. ID: {PartyId}", result.PartyId);

        var response = new DeletePartyResponse
        {
            PartyId = result.PartyId ?? id,
            Message = result.Message
        };

        return Success(response, "Party deactivated successfully");
    }

    /// <summary>
    /// Get list of parties with pagination and search
    /// </summary>
    /// <param name="BusinessId">Business Id is Nullable</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="searchTerm">Optional search term to filter by name, email, or TIN</param>
    /// <param name="sortBy">Optional sort field (name, email, createdat)</param>
    /// <param name="sortDescending">Sort in descending order (default: false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of parties</returns>
    [HttpGet]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetListAsync(
        [FromQuery] Guid? BusinessId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving parties list - Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}",
            pageNumber, pageSize, searchTerm ?? "None");

        // Validate pagination parameters
        if (pageNumber < 1)
        {
            return BadRequest(Error("Page number must be greater than 0"));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(Error("Page size must be between 1 and 100"));
        }

        var query = new GetPartyListQuery(BusinessId, pageNumber, pageSize, searchTerm, sortBy, sortDescending);
        var result = await _mediator.Send(query, cancellationToken);

        var mappedItems = (result?.Items ?? []).Select(p => new PartySummaryResponse
        {
            Id = p.Id,
            Name = p.Name,
            Email = p.Email,
            Phone = p.Phone,
            TaxIdentificationNumber = p.TaxIdentificationNumber,
            CreatedAt = p.CreatedAt
        }).ToList();

        var message = string.IsNullOrWhiteSpace(searchTerm)
            ? $"Retrieved {mappedItems.Count} parties (Page {pageNumber} of {pageSize} items)"
            : $"Found {mappedItems.Count} parties matching '{searchTerm}' (Page {pageNumber} of {pageSize} items)";

        return Success(new
        {
            items = mappedItems,
            totalCount = result?.TotalCount ?? 0,
            pageNumber = result?.PageNumber ?? pageNumber,
            pageSize = result?.PageSize ?? pageSize,
            totalPages = result?.TotalPages ?? 1
        }, message);
    }

    /// <summary>
    /// Get parties by business ID with pagination and search
    /// </summary>
    /// <param name="businessId">Business ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="searchTerm">Optional search term to filter by name, email, or TIN</param>
    /// <param name="sortBy">Optional sort field (name, email, createdat)</param>
    /// <param name="sortDescending">Sort in descending order (default: false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of parties for the specified business</returns>
    [HttpGet("business/{businessId:guid}")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> GetByBusinessIdAsync(
        [FromRoute] Guid businessId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving parties for business {BusinessId} - Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}",
            businessId, pageNumber, pageSize, searchTerm ?? "None");

        // Validate pagination parameters
        if (pageNumber < 1)
        {
            return BadRequest(Error("Page number must be greater than 0"));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(Error("Page size must be between 1 and 100"));
        }

        var query = new GetPartiesByBusinessIdQuery(pageNumber, pageSize, searchTerm, sortBy, sortDescending);
        var result = await _mediator.Send(query, cancellationToken);

        var mappedItems = (result?.Items ?? []).Select(p => new PartySummaryResponse
        {
            Id = p.Id,
            Name = p.Name,
            Email = p.Email,
            Phone = p.Phone,
            TaxIdentificationNumber = p.TaxIdentificationNumber,
            CreatedAt = p.CreatedAt
        }).ToList();

        var message = string.IsNullOrWhiteSpace(searchTerm)
            ? $"Retrieved {result?.TotalCount ?? 0} parties (Page {pageNumber} of {result?.TotalPages ?? 1})"
            : $"Found {result?.TotalCount ?? 0} parties matching '{searchTerm}'";

        return Success(new
        {
            items = mappedItems,
            totalCount = result?.TotalCount ?? 0,
            pageNumber = result?.PageNumber ?? pageNumber,
            pageSize = result?.PageSize ?? pageSize,
            totalPages = result?.TotalPages ?? 1
        }, message);
    }
}