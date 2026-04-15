using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.ItemCategory.Request;
using AegisEInvoicing.Portal.API.Models.ItemCategory.Response;
using AegisEInvoicing.Application.Features.ItemCategoryManagement.Commands.CreateItemCategory;
using AegisEInvoicing.Application.Features.ItemCategoryManagement.Commands.UpdateItemCategory;
using AegisEInvoicing.Application.Features.ItemCategoryManagement.Queries.GetItemCategoryById;
using AegisEInvoicing.Application.Features.ItemCategoryManagement.Queries.GetItemCategoryList;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for item category management operations
/// Handles CRUD operations for item categories within business context
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")][Authorize]
public class ItemCategoryController(IMediator mediator, ILogger<ItemCategoryController> logger) : BaseApiController
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<ItemCategoryController> _logger = logger;

    /// <summary>
    /// Create a new item category
    /// </summary>
    /// <param name="request">Item category creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created item category information</returns>
    [HttpPost]    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateItemCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating item category with name: {Name}", request.Name);

        var command = new CreateItemCategoryCommand(request.Name, request.Description);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to create item category: {Message}", result.Message);
            return BadRequest(Error(result.Message));
        }

        _logger.LogInformation("Item category created successfully. ID: {ItemCategoryId}", result.ItemCategoryId);

        var response = new CreateItemCategoryResponse
        {
            ItemCategoryId = result.ItemCategoryId ?? Guid.Empty, // Use Empty GUID if ItemCategoryId is null (shouldn't happen for successful create)
            Message = result.Message
        };

        return Created($"/api/v1/itemcategory/{result.ItemCategoryId}", Success(response, "Item category created successfully"));
    }

    /// <summary>
    /// Get item category by ID
    /// </summary>
    /// <param name="id">Item category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Item category details</returns>
    [HttpGet("{id:guid}")]    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving item category with ID: {Id}", id);

        var query = new GetItemCategoryByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success || result.ItemCategory == null)
        {
            _logger.LogWarning("Item category not found with ID: {Id}", id);
            return Error(result.Message, 404);
        }

        var response = new ItemCategoryResponse
        {
            Id = result.ItemCategory.Id,
            Name = result.ItemCategory.Name,
            Description = result.ItemCategory.Description,
            CreatedAt = result.ItemCategory.CreatedAt
        };

        return Success(response, "Item category retrieved successfully");
    }

    /// <summary>
    /// Update an existing item category
    /// </summary>
    /// <param name="id">Item category ID</param>
    /// <param name="request">Updated item category details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result</returns>
    [HttpPut("{id:guid}")]    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> UpdateAsync([FromRoute] Guid id, [FromBody] UpdateItemCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating item category with ID: {Id}", id);

        var command = new UpdateItemCategoryCommand(id, request.Name, request.Description);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to update item category: {Message}", result.Message);
            return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) 
                ? Error(result.Message, 404) 
                : BadRequest(Error(result.Message));
        }

        _logger.LogInformation("Item category updated successfully. ID: {ItemCategoryId}", result.ItemCategoryId);

        var response = new UpdateItemCategoryResponse
        {
            ItemCategoryId = result.ItemCategoryId ?? id, // Use the original ID if ItemCategoryId is null
            Message = result.Message
        };

        return Success(response, "Item category updated successfully");
    }

    /// <summary>
    /// Get list of item categories with pagination and search
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="searchTerm">Optional search term to filter by name or description</param>
    /// <param name="sortBy">Optional sort field (name, description, createdat)</param>
    /// <param name="sortDescending">Sort in descending order (default: false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of item categories</returns>
    [HttpGet]    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetListAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving item categories list - Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}", 
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

        var query = new GetItemCategoryListQuery(pageNumber, pageSize, searchTerm, sortBy, sortDescending);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null || !result.Items.Any())
        {
            return Success(Enumerable.Empty<ItemCategoryResponse>(), "No item categories found");
        }

        var response = result.Items.Select(ic => new ItemCategoryResponse
        {
            Id = ic.Id,
            Name = ic.Name,
            Description = ic.Description,
            CreatedAt = ic.CreatedAt
        });

        var message = string.IsNullOrWhiteSpace(searchTerm) 
            ? $"Retrieved {result.Items.Count} item categories (Page {pageNumber} of {pageSize} items)"
            : $"Found {result.Items.Count} item categories matching '{searchTerm}' (Page {pageNumber} of {pageSize} items)";

        return Success(response, message);
    }
}