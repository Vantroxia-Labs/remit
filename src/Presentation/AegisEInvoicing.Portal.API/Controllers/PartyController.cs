using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.Party.Request;
using AegisEInvoicing.Portal.API.Models.Party.Response;
using AegisEInvoicing.Application.Features.PartyManagement.Commands.CreateBulkParty;
using AegisEInvoicing.Application.Features.PartyManagement.Commands.CreateParty;
using AegisEInvoicing.Application.Features.PartyManagement.Commands.DeleteParty;
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
using Swashbuckle.AspNetCore.Annotations;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for party management operations
/// Handles CRUD operations for parties (customers, suppliers, etc.) within business context
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[SwaggerTag("Party Management Operations including create, read, update, delete and list parties")]
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
    [SwaggerOperation(
        Summary = "Create Party",
        Description = "Creates a new party (customer, supplier, etc.) for the current business. Only business administrators with SaaS subscription can create parties."
    )]
    [SwaggerResponse(201, "Party created successfully", typeof(ApiResponse<CreatePartyResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Insufficient permissions - Requires SaaS subscription", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
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
            request.Address.PostalCode);

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
    [SwaggerOperation(Summary = "Create bulk party", Description = "Upload a file of data to creates a mulitple new party (customer, supplier, etc.) for the current business. Only business administrators with SaaS subscription can create parties."
    )]
    [SwaggerResponse(200, "Request successfully", typeof(ApiResponse<CreatePartyResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Insufficient permissions - Requires SaaS subscription", typeof(ApiResponse<object>))]    
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
    [SwaggerOperation(
        Summary = "Get Party by ID",
        Description = "Retrieves a specific party by its ID. Only parties belonging to the current business can be accessed."
    )]
    [SwaggerResponse(200, "Party retrieved successfully", typeof(ApiResponse<PartyResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Party not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
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
                PostalCode = result.Party.Address.PostalCode
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
    [SwaggerOperation(
        Summary = "Update Party",
        Description = "Updates an existing party. Only business administrators with SaaS subscription can update parties."
    )]
    [SwaggerResponse(200, "Party updated successfully", typeof(ApiResponse<UpdatePartyResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Insufficient permissions - Requires SaaS subscription", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Party not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
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
            request.Address.PostalCode);

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

    [SwaggerOperation(
        Summary = "Validate Party Fields",
        Description = "Validates whether party fields (TaxIdentificationNumber) exist in the system."
    )]
    [SwaggerResponse(200, "Validation completed successfully", typeof(ApiResponse<PartyValidationResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<object>))]
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
    /// Delete a party
    /// </summary>
    /// <param name="id">Party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireSaasSubscription")]
    [SwaggerOperation(
        Summary = "Delete Party",
        Description = "Deletes a party. Only business administrators with SaaS subscription can delete parties."
    )]
    [SwaggerResponse(200, "Party deleted successfully", typeof(ApiResponse<DeletePartyResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Insufficient permissions - Requires SaaS subscription", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Party not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> DeleteAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting party with ID: {Id}", id);

        var command = new DeletePartyCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to delete party: {Message}", result.Message);
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) 
                ? Error(result.Message, 404) 
                : BadRequest(Error(result.Message));
        }

        _logger.LogInformation("Party deleted successfully. ID: {PartyId}", result.PartyId);

        var response = new DeletePartyResponse
        {
            PartyId = result.PartyId ?? id, // Use the original ID if PartyId is null
            Message = result.Message
        };

        return Success(response, "Party deleted successfully");
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
    [SwaggerOperation(
        Summary = "Get Parties List",
        Description = @"Retrieve all parties for the current business with pagination, search, and sorting capabilities.

**Features:**
- **Pagination**: Use pageNumber and pageSize parameters
- **Search**: Filter by name, email, or tax identification number using searchTerm  
- **Sorting**: Sort by name, email, or createdat
- **Security**: Only returns parties belonging to the current business

**Query Parameters:**
- `pageNumber`: Page number (default: 1)
- `pageSize`: Items per page (default: 10, max: 100)
- `searchTerm`: Search in name, email, and TIN
- `sortBy`: Field to sort by (name, email, createdat)
- `sortDescending`: Sort order (default: false - ascending)

**Access Control:**
- **Business Admin**: Can view all parties for their business
- **Business User**: Can view all parties for their business"
    )]
    [SwaggerResponse(200, "Request successful", typeof(ApiResponse<IEnumerable<PartySummaryResponse>>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
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

        if (result == null || !result.Items.Any())
        {
            return Success(Enumerable.Empty<PartySummaryResponse>(), "No parties found");
        }

        var response = result.Items.Select(p => new PartySummaryResponse
        {
            Id = p.Id,
            Name = p.Name,
            Email = p.Email,
            TaxIdentificationNumber = p.TaxIdentificationNumber,
            CreatedAt = p.CreatedAt
        });

        var message = string.IsNullOrWhiteSpace(searchTerm) 
            ? $"Retrieved {result.Items.Count} parties (Page {pageNumber} of {pageSize} items)"
            : $"Found {result.Items.Count} parties matching '{searchTerm}' (Page {pageNumber} of {pageSize} items)";

        return Success(response, message);
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
    [SwaggerOperation(
        Summary = "Get Parties by Business ID",
        Description = @"Retrieve all parties for a specific business with pagination, search, and sorting capabilities.
        
**Important:** This endpoint is primarily for KMPG administrators to view parties across different businesses. 
Business administrators will typically use the regular GET endpoint which automatically filters to their business.

**Features:**
- **Pagination**: Use pageNumber and pageSize parameters
- **Search**: Filter by name, email, or tax identification number using searchTerm  
- **Sorting**: Sort by name, email, or createdat
- **Cross-Business Access**: KMPG admins can view parties from any business

**Query Parameters:**
- `businessId`: Specific business ID to retrieve parties from
- `pageNumber`: Page number (default: 1)
- `pageSize`: Items per page (default: 10, max: 100)
- `searchTerm`: Search in name, email, and TIN
- `sortBy`: Field to sort by (name, email, createdat)
- `sortDescending`: Sort order (default: false - ascending)

**Access Control:**
- **KMPG Admin**: Can view parties for any business
- **Business Admin**: Can view parties for their own business only"
    )]
    [SwaggerResponse(200, "Request successful", typeof(ApiResponse<IEnumerable<PartySummaryResponse>>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
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

        if (result == null || !result.Items.Any())
        {
            return Success(Enumerable.Empty<PartySummaryResponse>(), "No parties found for the specified business");
        }

        var response = result.Items.Select(p => new PartySummaryResponse
        {
            Id = p.Id,
            Name = p.Name,
            Email = p.Email,
            TaxIdentificationNumber = p.TaxIdentificationNumber,
            CreatedAt = p.CreatedAt
        });

        var message = string.IsNullOrWhiteSpace(searchTerm) 
            ? $"Retrieved {result.Items.Count} parties for business {businessId} (Page {pageNumber} of {pageSize} items)"
            : $"Found {result.Items.Count} parties for business {businessId} matching '{searchTerm}' (Page {pageNumber} of {pageSize} items)";

        return Success(response, message);
    }
}