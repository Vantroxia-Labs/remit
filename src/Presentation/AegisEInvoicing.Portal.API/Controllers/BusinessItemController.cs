using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.BusinessItem.Request;
using AegisEInvoicing.Portal.API.Models.BusinessItem.Response;
using AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.CreateBulkBusinessItem;
using AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.CreateBusinessItem;
using AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.DeleteBusinessItem;
using AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.UpdateBusinessItem;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using AegisEInvoicing.Application.Features.BusinessItemManagement.Queries.GetBusinessItemById;
using AegisEInvoicing.Application.Features.BusinessItemManagement.Queries.GetBusinessItemList;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for business item management operations
/// Handles CRUD operations for business items including products and services
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[SwaggerTag("Business Item Operations including create, read, update, delete and list items")]
[Authorize]
public class BusinessItemController(IMediator mediator, ILogger<BusinessItemController> logger) : BaseApiController
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<BusinessItemController> _logger = logger;

    /// <summary>
    /// Create a new business item
    /// </summary>
    /// <param name="request">Business item creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created business item information</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create Business Item",
        Description = "Creates a new business item (product or service) for the current business. Only business administrators can create items."
    )]
    [SwaggerResponse(201, "Business item created successfully", typeof(ApiResponse<CreateBusinessItemResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateBusinessItemRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating business item with name: {Name}", request.Name);

        var serviceCodeDto = new CreateServiceCodeDto(request.ServiceCode.Code.Trim(), request.ServiceCode.Name.Trim());
        var taxCategoryDto = new CreateTaxCategoryDto(request.TaxCategory.Name.Trim(), request.TaxCategory.Percent);

        var command = new CreateBusinessItemCommand(
            request.Name.Trim(),
            serviceCodeDto,
            taxCategoryDto,
            request.ItemCategoryId,
            request.ItemDescription,
            request.UnitPrice);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to create business item: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        _logger.LogInformation("Business item created successfully. ID: {BusinessItemId}", result.BusinessItemId);

        var response = new CreateBusinessItemResponse
        {
            BusinessItemId = result.BusinessItemId ?? Guid.Empty, // Use Empty GUID if BusinessItemId is null (shouldn't happen for successful create)
            Message = result.Message
        };

        return Created($"/api/v1/businessitem/{result.BusinessItemId}", Success(response, "Business item created successfully"));
    }

    /// <summary>
    /// This endpoint allows business admin to upload an excel to create multiple bulk items
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("CreateBulkItem")]
    [SwaggerOperation(Summary = "Create new bulk item", Description = "Creates a new bulk business item (product or service) for the current business. Only business administrators can create items."
    )]
    [SwaggerResponse(200, "Bulk item created successfully", typeof(ApiResponse<BulkItemResult>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Insufficient permissions", typeof(ApiResponse<object>))]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> CreateBulkItem(CreateBulkBusinessItemUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Client admin can call this endpoint to create a list of bulk item");

        var command = new CreateBulkBusinessItemCommand(request.file);

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to create multiple bulk item withh: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }

    /// <summary>
    /// Get business item by ID
    /// </summary>
    /// <param name="id">Business item ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Business item details</returns>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get Business Item by ID",
        Description = "Retrieves a specific business item by its ID. Only items belonging to the current business can be accessed."
    )]
    [SwaggerResponse(200, "Business item retrieved successfully", typeof(ApiResponse<BusinessItemResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Business item not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving business item with ID: {Id}", id);

        var query = new GetBusinessItemByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.BusinessItem == null)
        {
            _logger.LogWarning("Business item not found with ID: {Id}", id);
            return Error(result.Message, 404);
        }

        var response = new BusinessItemResponse
        {
            Id = result.BusinessItem.Id,
            ItemId = result.BusinessItem.ItemId,
            Name = result.BusinessItem.Name,
            ServiceCode = new ServiceCodeResponse
            {
                Code = result.BusinessItem.ServiceCode.Code,
                Name = result.BusinessItem.ServiceCode.Name
            },
            TaxCategory = new TaxCategoryResponse
            {
                Name = result.BusinessItem.TaxCategory.Name,
                Percent = result.BusinessItem.TaxCategory.Percent
            },
            ItemCategoryId = result.BusinessItem.ItemCategoryId,
            ItemCategoryName = result.BusinessItem.ItemCategoryName,
            ItemDescription = result.BusinessItem.ItemDescription,
            UnitPrice = result.BusinessItem.UnitPrice,
            BusinessId = result.BusinessItem.BusinessId,
            BusinessName = result.BusinessItem.BusinessName,
            CreatedAt = result.BusinessItem.CreatedAt,
            UpdatedAt = result.BusinessItem.UpdatedAt,
            CreatedBy = result.BusinessItem.CreatedBy,
            UpdatedBy = result.BusinessItem.UpdatedBy
        };

        return Success(response, "Business item retrieved successfully");
    }

    /// <summary>
    /// Update an existing business item
    /// </summary>
    /// <param name="id">Business item ID</param>
    /// <param name="request">Updated business item details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result</returns>
    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        Summary = "Update Business Item",
        Description = "Updates an existing business item. Only business administrators can update items belonging to their business."
    )]
    [SwaggerResponse(200, "Business item updated successfully", typeof(ApiResponse<UpdateBusinessItemResponse>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Business item not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> UpdateAsync([FromRoute] Guid id, [FromBody] UpdateBusinessItemRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating business item with ID: {Id}", id);

        var serviceCodeDto = new UpdateServiceCodeDto(request.ServiceCode.Code, request.ServiceCode.Name);
        var taxCategoryDto = new UpdateTaxCategoryDto(request.TaxCategory.Name, request.TaxCategory.Percent);

        var command = new UpdateBusinessItemCommand(
            id,
            request.Name,
            serviceCodeDto,
            taxCategoryDto,
            request.ItemCategoryId,
            request.ItemDescription,
            request.UnitPrice);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to update business item: {Message}", result.Message);
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Error(result.Message, 404)
                : BadRequest(Error(result.Message));
        }

        _logger.LogInformation("Business item updated successfully. ID: {BusinessItemId}", result.BusinessItemId);

        var response = new UpdateBusinessItemResponse
        {
            BusinessItemId = result.BusinessItemId ?? id, // Use the original ID if BusinessItemId is null
            Message = result.Message
        };

        return Success(response, "Business item updated successfully");
    }

    /// <summary>
    /// Delete a business item
    /// </summary>
    /// <param name="id">Business item ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Delete Business Item",
        Description = "Soft deletes a business item. Only business administrators can delete items. The item will be marked as deleted but retained for audit purposes."
    )]
    [SwaggerResponse(200, "Business item deleted successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Business item not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> DeleteAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting business item with ID: {Id}", id);

        var command = new DeleteBusinessItemCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to delete business item: {Message}", result.Message);
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Error(result.Message, 404)
                : BadRequest(Error(result.Message));
        }

        _logger.LogInformation("Business item deleted successfully. ID: {Id}", id);
        return Success<object?>(null, "Business item deleted successfully");
    }

    /// <summary>
    /// Get list of business items with pagination and search
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="searchTerm">Optional search term to filter by name, ID, or description</param>
    /// <param name="itemCategoryId">Optional filter by item category</param>
    /// <param name="sortBy">Optional sort field (name, itemid, unitprice, category, createdat)</param>
    /// <param name="sortDescending">Sort in descending order (default: false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of business items</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get Business Items List",
        Description = @"Retrieve all business items for the current business with pagination, search, and sorting capabilities.

**Features:**
- **Pagination**: Use pageNumber and pageSize parameters
- **Search**: Filter by name, item ID, description, service code, or category
- **Category Filter**: Filter items by specific category ID
- **Sorting**: Sort by name, itemid, unitprice, category, or createdat
- **Security**: Only returns items belonging to the current business

**Query Parameters:**
- `pageNumber`: Page number (default: 1)
- `pageSize`: Items per page (default: 10, max: 100)
- `searchTerm`: Search in name, ID, description, service code, and category
- `itemCategoryId`: Filter by specific item category
- `sortBy`: Field to sort by (name, itemid, unitprice, category, createdat)
- `sortDescending`: Sort order (default: false - ascending)

**Access Control:**
- **Business Admin**: Can view all items for their business
- **Business User**: Can view all items for their business"
    )]
    [SwaggerResponse(200, "Request successful", typeof(ApiResponse<IEnumerable<BusinessItemSummaryResponse>>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetListAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? itemCategoryId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving business items list - Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}, Category: {CategoryId}",
            pageNumber, pageSize, searchTerm ?? "None", itemCategoryId?.ToString() ?? "None");

        // Validate pagination parameters
        if (pageNumber < 1)
        {
            return BadRequest(Error("Page number must be greater than 0"));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(Error("Page size must be between 1 and 100"));
        }

        var query = new GetBusinessItemListQuery(pageNumber, pageSize, searchTerm, sortBy, sortDescending, itemCategoryId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result is null || !result.Items.Any())
        {
            return Success(result, "No business items found");
        }

        var message = string.IsNullOrWhiteSpace(searchTerm)
            ? $"Retrieved {result.Items.Count} business items (Page {pageNumber} of {pageSize} items)"
            : $"Found {result.Items.Count} business items matching '{searchTerm}' (Page {pageNumber} of {pageSize} items)";

        return Success(result, message);
    }
}