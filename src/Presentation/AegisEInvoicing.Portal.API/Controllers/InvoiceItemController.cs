using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoiceItem;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.DeleteInvoiceItem;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoiceItem;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceItemById;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceItemsByInvoiceId;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for invoice item management operations
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/invoice-items")]
[SwaggerTag("Invoice Item Management Operations - Create, Read, Update, Delete invoice items")]
[Authorize]
public class InvoiceItemController(IMediator mediator, ILogger<InvoiceItemController> logger) : BaseApiController
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<InvoiceItemController> _logger = logger;

    /// <summary>
    /// Add a new item to an existing invoice
    /// </summary>
    /// <param name="command">Invoice item creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created invoice item details</returns>
    [HttpPost]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Create invoice item",
        Description = "Adds a new item to an existing draft invoice"
    )]
    [SwaggerResponse(201, "Invoice item created successfully", typeof(ApiResponse<CreateInvoiceItemResult>))]
    [SwaggerResponse(400, "Invalid request or invoice is not draft", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
    public async Task<IActionResult> CreateInvoiceItem(
        [FromBody] CreateInvoiceItemCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Invoice item created successfully with ID: {InvoiceItemId} for Invoice: {InvoiceId}", 
                    result.InvoiceItemId, command.InvoiceId);
                    
                return CreatedAtAction(
                    nameof(GetInvoiceItemById),
                    new { id = result.InvoiceItemId },
                    Success(result, "Invoice item created successfully"));
            }

            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(Error(result.Message));
            }

            return BadRequest(Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice item for Invoice: {InvoiceId}", command.InvoiceId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while creating the invoice item"));
        }
    }

    /// <summary>
    /// Get invoice item by ID
    /// </summary>
    /// <param name="id">Invoice item ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Invoice item details</returns>
    [HttpGet("{id}")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Get invoice item by ID",
        Description = "Retrieves detailed invoice item information"
    )]
    [SwaggerResponse(200, "Invoice item found", typeof(ApiResponse<GetInvoiceItemByIdResult>))]
    [SwaggerResponse(404, "Invoice item not found", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GetInvoiceItemById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetInvoiceItemByIdQuery { InvoiceItemId = id };
            var result = await _mediator.Send(query, cancellationToken);

            if (result.Success && result.InvoiceItem != null)
            {
                return Ok(Success(result, "Invoice item retrieved successfully"));
            }

            return NotFound(Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice item with ID: {InvoiceItemId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while retrieving the invoice item"));
        }
    }

    /// <summary>
    /// Get all items for a specific invoice
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of invoice items</returns>
    [HttpGet("by-invoice/{invoiceId}")]
    [RequireRole(RoleConstants.AegisAdmin, RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Get invoice items by invoice ID",
        Description = "Retrieves all items for a specific invoice"
    )]
    [SwaggerResponse(200, "Invoice items retrieved successfully", typeof(ApiResponse<GetInvoiceItemsByInvoiceIdResult>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GetInvoiceItemsByInvoiceId(
        [FromRoute] Guid invoiceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetInvoiceItemsByInvoiceIdQuery { InvoiceId = invoiceId };
            var result = await _mediator.Send(query, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Retrieved {Count} invoice items for Invoice: {InvoiceId}", 
                    result.InvoiceItems.Count, invoiceId);
                    
                return Ok(Success(result, "Invoice items retrieved successfully"));
            }

            return NotFound(Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice items for Invoice ID: {InvoiceId}", invoiceId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while retrieving invoice items"));
        }
    }

    /// <summary>
    /// Update an existing invoice item
    /// </summary>
    /// <param name="id">Invoice item ID</param>
    /// <param name="command">Update command (without ID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result</returns>
    [HttpPut("{id}")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Update invoice item",
        Description = "Updates an existing invoice item (only in draft invoices). Supports partial updates for quantity, discount, and price."
    )]
    [SwaggerResponse(200, "Invoice item updated successfully", typeof(ApiResponse<UpdateInvoiceItemResult>))]
    [SwaggerResponse(400, "Invalid request or invoice item cannot be updated", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice item not found", typeof(ApiResponse<object>))]
    public async Task<IActionResult> UpdateInvoiceItem(
        [FromRoute] Guid id,
        [FromBody] UpdateInvoiceItemCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var commandWithId = command with { InvoiceItemId = id };
            var result = await _mediator.Send(commandWithId, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Invoice item updated successfully with ID: {InvoiceItemId}", id);
                return Ok(Success(result, "Invoice item updated successfully"));
            }

            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(Error(result.Message));
            }

            return BadRequest(Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice item with ID: {InvoiceItemId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while updating the invoice item"));
        }
    }

    /// <summary>
    /// Delete an invoice item
    /// </summary>
    /// <param name="id">Invoice item ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    [RequireRole(RoleConstants.ClientAdmin)]
    [SwaggerOperation(
        Summary = "Delete invoice item",
        Description = "Deletes an invoice item (only from draft invoices)"
    )]
    [SwaggerResponse(200, "Invoice item deleted successfully", typeof(ApiResponse<DeleteInvoiceItemResult>))]
    [SwaggerResponse(400, "Invoice item cannot be deleted", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice item not found", typeof(ApiResponse<object>))]
    public async Task<IActionResult> DeleteInvoiceItem(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteInvoiceItemCommand { InvoiceItemId = id };
            var result = await _mediator.Send(command, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Invoice item deleted successfully with ID: {InvoiceItemId}", id);
                return Ok(Success(result, "Invoice item deleted successfully"));
            }

            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(Error(result.Message));
            }

            return BadRequest(Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice item with ID: {InvoiceItemId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while deleting the invoice item"));
        }
    }

    /// <summary>
    /// Bulk create invoice items
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="items">List of items to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of created invoice items</returns>
    [HttpPost("bulk")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Bulk create invoice items",
        Description = "Creates multiple invoice items for a draft invoice in a single operation"
    )]
    [SwaggerResponse(201, "Invoice items created successfully", typeof(ApiResponse<List<CreateInvoiceItemResult>>))]
    [SwaggerResponse(400, "Invalid request or invoice is not draft", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
    public async Task<IActionResult> BulkCreateInvoiceItems(
        [FromQuery] Guid invoiceId,
        [FromBody] List<CreateInvoiceItemCommand> items,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = new List<CreateInvoiceItemResult>();
            var failedItems = new List<string>();

            foreach (var item in items)
            {
                var command = item with { InvoiceId = invoiceId };
                var result = await _mediator.Send(command, cancellationToken);
                
                if (result.Success)
                {
                    results.Add(result);
                }
                else
                {
                    failedItems.Add($"Item '{item.BusinessItemId}' Unknown : {result.Message}");
                }
            }

            if (failedItems.Any())
            {
                _logger.LogWarning("Bulk create partially failed. {FailedCount} items failed out of {TotalCount}", 
                    failedItems.Count, items.Count);
                    
                return StatusCode(StatusCodes.Status207MultiStatus, 
                    Success(new { CreatedItems = results, FailedItems = failedItems }, 
                    $"Partial success: {results.Count} items created, {failedItems.Count} failed"));
            }

            _logger.LogInformation("Bulk created {Count} invoice items for Invoice: {InvoiceId}", 
                results.Count, invoiceId);
                
            return CreatedAtAction(
                nameof(GetInvoiceItemsByInvoiceId),
                new { invoiceId },
                Success(results, $"Successfully created {results.Count} invoice items"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating invoice items for Invoice: {InvoiceId}", invoiceId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while creating invoice items"));
        }
    }

    /// <summary>
    /// Bulk delete invoice items
    /// </summary>
    /// <param name="itemIds">List of invoice item IDs to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Delete results</returns>
    [HttpDelete("bulk")]
    [RequireRole(RoleConstants.ClientAdmin)]
    [SwaggerOperation(
        Summary = "Bulk delete invoice items",
        Description = "Deletes multiple invoice items from draft invoices in a single operation"
    )]
    [SwaggerResponse(200, "Invoice items deleted successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(207, "Partial success - some items deleted", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
    public async Task<IActionResult> BulkDeleteInvoiceItems(
        [FromBody] List<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var deletedCount = 0;
            var failedItems = new List<string>();

            foreach (var itemId in itemIds)
            {
                var command = new DeleteInvoiceItemCommand { InvoiceItemId = itemId };
                var result = await _mediator.Send(command, cancellationToken);
                
                if (result.Success)
                {
                    deletedCount++;
                }
                else
                {
                    failedItems.Add($"Item {itemId}: {result.Message}");
                }
            }

            if (failedItems.Any())
            {
                _logger.LogWarning("Bulk delete partially failed. {FailedCount} items failed out of {TotalCount}", 
                    failedItems.Count, itemIds.Count);
                    
                return StatusCode(StatusCodes.Status207MultiStatus, 
                    Success(new { DeletedCount = deletedCount, FailedItems = failedItems }, 
                    $"Partial success: {deletedCount} items deleted, {failedItems.Count} failed"));
            }

            _logger.LogInformation("Bulk deleted {Count} invoice items", deletedCount);
            
            return Ok(Success(new { DeletedCount = deletedCount }, 
                $"Successfully deleted {deletedCount} invoice items"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deleting invoice items");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while deleting invoice items"));
        }
    }
}