using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.Invoice.Request;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Common.Models.InvoiceData;
using AegisEInvoicing.Application.Extensions;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.DeleteInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.DeleteInvoiceDraft;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SaveInvoiceDraft;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoicePaymentStatus;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UploadInvoices;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetAllInvoices;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceDrafts;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceById;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceIrns;
using AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Commands.UpdateReceivedInvoicePaymentStatus;
using AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.DTOs;
using AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Queries.GetReceivedInvoiceById;
using AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Queries.GetReceivedInvoices;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.ValueObjects;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for invoice management operations
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public partial class InvoiceController(IMediator mediator, ILogger<InvoiceController> logger) : BaseApiController
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<InvoiceController> _logger = logger;

    /// <summary>
    /// Creates a new invoice with invoice items compliant with FIRS UBL format
    /// </summary>
    /// <param name="createInvoiceRequest">Invoice creation details including party, items, payment terms, and tax information</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created invoice with IRN (Invoice Reference Number) and validation status</returns>
    [HttpPost]
    [EnableRateLimiting("InvoiceCreation")] // Strict rate limit: 10 invoices per minute per user
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> CreateInvoice(
        [FromBody] CreateInvoiceRequest createInvoiceRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateInvoiceCommand
            {
                PartyId = createInvoiceRequest.PartyId,
                IssueDate = createInvoiceRequest.IssueDate,
                InvoiceType = InvoiceType.Create(createInvoiceRequest.InvoiceType.Name,
                                                createInvoiceRequest.InvoiceType.Code),
                Currency = Currency.Create(createInvoiceRequest.Currency.Name,
                                          createInvoiceRequest.Currency.Code),
                DeliveryPeriod = DeliveryPeriod.Create(createInvoiceRequest.DeliveryPeriod.StartDate,
                                                      createInvoiceRequest.DeliveryPeriod.EndDate),
                BillingReferences = createInvoiceRequest.BillingReference,
                DueDate = createInvoiceRequest.DueDate,
                PaymentMeans = PaymentMeans.Create(createInvoiceRequest.PaymentMeans.Code,
                                                      createInvoiceRequest.PaymentMeans.Name),
                Note = createInvoiceRequest.Note,
                PaymentReference = createInvoiceRequest.PaymentReference,
                PaymentTerms = createInvoiceRequest.PaymentTerms,
                InvoiceItems = createInvoiceRequest.InvoiceItems,
                InvoiceKind = createInvoiceRequest.InvoiceKind,
                DispatchDocumentReference = createInvoiceRequest.DispatchDocumentReference,
                ReceiptDocumentReference = createInvoiceRequest.ReceiptDocumentReference,
                OriginatorDocumentReference = createInvoiceRequest.OriginatorDocumentReference,
                ContractDocumentReference = createInvoiceRequest.ContractDocumentReference,
                AdditionalDocumentReferences = createInvoiceRequest.AdditionalDocumentReferences
            };

            var result = await _mediator.Send(command, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, $"Error Message {ex}");
            return BadRequest(ex.Message);
        }
    }


    /// <summary>
    /// Bulk upload invoices from Excel file
    /// </summary>
    /// <param name="invoicesUpload">Excel file containing multiple invoices (.xlsx or .xls)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Upload result with count of successful and failed invoices</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(500_000_000)] // 500MB limit
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> UploadInvoice(
        IFormFile invoicesUpload,
        CancellationToken cancellationToken)
    {
        try
        {
            if (invoicesUpload == null || invoicesUpload.Length == 0)
                return Error("No file uploaded", HttpStatusCodes.BadRequest.ToInt());

            if (!IsExcelFile(invoicesUpload))
                return Error("Only Excel files (.xlsx, .xls) are supported", HttpStatusCodes.BadRequest.ToInt());

            var options = new StreamingConversionOptions
            {
                PrettyPrint = false,
                IncludeNullValues = false,
                BatchSize = 1000,
                DateTimeFormat = "yyyy-MM-dd"
            };

            var progress = new Progress<ConversionProgress>(p =>
            {
                if (p.IsComplete)
                {
                    _logger.LogInformation(
                        "Conversion complete: {Elapsed:F2}s",
                        p.ElapsedTime.TotalSeconds);
                }
            });

            var invoices = await StreamingInvoiceConverter
                .ConvertToTypedListWithGroupingAsync<UploadInvoiceRequest>(
                    invoicesUpload,
                    options,
                    progress);

            _logger.LogInformation("Successfully converted {Count} invoices", invoices.Count);

            var command = new UploadInvoiceCommand(invoices);

            var result = await _mediator.Send(command, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes, result);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON deserialization error");
            return BadRequest(new
            {
                error = "Excel data doesn't match the expected invoice format",
                details = jsonEx.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting Excel file");
            return StatusCode(500, new { error = ex.Message });
        }
    }


    /// <summary>
    /// Retrieves detailed invoice information by ID
    /// </summary>
    /// <param name="id">Invoice unique identifier (GUID)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Complete invoice details including items, party information, and status</returns>
    [HttpGet("{id}")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetInvoiceById(
         Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetInvoiceByIdQuery { InvoiceId = id };
            var result = await _mediator.Send(query, cancellationToken);

            if (result.Success && result.Invoice != null)
            {
                return Success(result, "Invoice retrieved successfully");
            }

            return NotFound(Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice with ID: {InvoiceId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while retrieving the invoice"));
        }
    }

    /// <summary>
    /// Get all invoices with pagination and filtering
    /// </summary>
    /// <param name="businessId">Optional business ID filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="environmentMode">Optional app environment mode filter</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="orderBy">Order by field (default: createdAt)</param>
    /// <param name="orderByDescending">Order descending (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of invoices</returns>
    [HttpGet]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetAllInvoices(
        [FromQuery] Guid? businessId,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? searchTerm,
        [FromQuery] AppEnvironmentMode? environmentMode,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? orderBy = "createdAt",
        [FromQuery] bool orderByDescending = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAllInvoicesQuery
            {
                BusinessId = businessId,
                InvoiceStatus = status,
                StartDate = startDate,
                EndDate = endDate,
                SearchTerm = searchTerm,
                EnvironmentMode = environmentMode,
                PageNumber = pageNumber,
                PageSize = pageSize,
                OrderBy = orderBy,
                OrderByDescending = orderByDescending
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Paginated(result, "Invoices retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while retrieving invoices"));
        }
    }


    /// <summary>
    /// Get all received invoices with pagination and filtering
    /// </summary>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="environmentMode">Optional environment mode</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="orderBy">Order by field (default: IssueDate)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of received invoices</returns>
    [HttpGet("received-invoices")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetAllReceivedInvoices(
        [FromQuery] string? searchTerm,
        [FromQuery] AppEnvironmentMode? environmentMode = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string orderBy = "IssueDate",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetReceivedInvoicesQuery
            {
                SearchTerm = searchTerm,
                EnvironmentMode = environmentMode,
                Page = pageNumber,
                PageSize = pageSize,
                SortBy = orderBy
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(Error(result.Message));
            }

            var paginatedList = new PaginatedList<ReceivedInvoiceListDto>(
                result.Invoices,
                result.TotalCount,
                result.Page,
                result.PageSize);

            return Paginated(paginatedList, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving received invoices");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while retrieving received invoices"));
        }
    }

    /// <summary>
    /// Get a single received invoice by ID with full details
    /// </summary>
    /// <param name="id">Received invoice unique identifier (GUID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete received invoice details including invoice lines and tax totals</returns>
    [HttpGet("received-invoices/{id}")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetReceivedInvoiceById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetReceivedInvoiceByIdQuery
            {
                InvoiceId = id
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (result.Success && result.Invoice != null)
            {
                return Success(result, "Received invoice retrieved successfully");
            }

            return NotFound(Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving received invoice with ID: {InvoiceId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while retrieving the received invoice"));
        }
    }

    /// <summary>
    /// Export invoices to Excel with filtering
    /// </summary>
    /// <param name="status">Optional invoice status filter</param>
    /// <param name="paymentStatus">Optional payment status filter</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="paymentReference">Optional payment reference filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Excel file with invoice data grouped by PaymentReference</returns>
    [HttpGet("export")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> ExportInvoices(
        [FromQuery] InvoiceStatus? status,
        [FromQuery] PaymentStatus? paymentStatus,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? searchTerm,
        [FromQuery] string? paymentReference,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new Application.Features.InvoiceManagement.Queries.ExportInvoices.ExportInvoicesQuery
            {
                InvoiceStatus = status,
                PaymentStatus = paymentStatus,
                StartDate = startDate,
                EndDate = endDate,
                SearchTerm = searchTerm,
                PaymentReference = paymentReference
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.FileContents == null)
            {
                return NotFound(Error(result.Message ?? "No invoices found to export"));
            }

            return File(
                result.FileContents,
                result.ContentType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.FileName ?? "Invoices_Export.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting invoices");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while exporting invoices"));
        }
    }


    /// <summary>
    /// Update an existing invoice
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="updateInvoiceRequest">Update command (without ID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result</returns>
    [HttpPut("{id}")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> UpdateInvoice(
        [FromRoute] Guid id,
        [FromBody] UpdateInvoiceRequest updateInvoiceRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateInvoiceCommand
            {
                PartyId = updateInvoiceRequest.PartyId,
                IssueDate = updateInvoiceRequest.IssueDate,
                InvoiceType = InvoiceType.Create(updateInvoiceRequest.InvoiceType.Name,
                                                updateInvoiceRequest.InvoiceType.Code),
                Currency = Currency.Create(updateInvoiceRequest.Currency.Name,
                                          updateInvoiceRequest.Currency.Code),
                DeliveryPeriod = DeliveryPeriod.Create(updateInvoiceRequest.DeliveryPeriod.StartDate,
                                                      updateInvoiceRequest.DeliveryPeriod.EndDate),
                DueDate = updateInvoiceRequest.DueDate,
                PaymentMeans = PaymentMeans.Create(updateInvoiceRequest.PaymentMeans.Code,
                                                      updateInvoiceRequest.PaymentMeans.Name),
                Note = updateInvoiceRequest.Note,
                PaymentReference = updateInvoiceRequest.PaymentReference,
                PaymentTerms = updateInvoiceRequest.PaymentTerms
            };

            var commandWithId = command with { InvoiceId = id };
            var result = await _mediator.Send(commandWithId, cancellationToken);

            if (result.Success)
            {
                return Success(result, "Invoice updated successfully");
            }

            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(Error(result.Message));
            }

            return BadRequest(Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice with ID: {InvoiceId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while updating the invoice"));
        }
    }

    /// <summary>
    /// Update an existing invoice payment status
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="updateInvoicePaymentStatusRequest">Update command (without ID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result</returns>
    [HttpPatch("update-payment-status/{id}")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> UpdateInvoicePaymentStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateInvoicePaymentStatusRequest updateInvoicePaymentStatusRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateInvoicePaymentStatusCommand(id, updateInvoicePaymentStatusRequest.PaymentStatus, updateInvoicePaymentStatusRequest.Reference);
            var result = await _mediator.Send(command, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice payment status with ID: {InvoiceId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while updating the invoice payment status"));
        }
    }

    /// <summary>
    /// Update a received invoice payment status (buyer action: PAID or REJECTED)
    /// </summary>
    [HttpPatch("received-invoices/update-payment-status/{id}")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> UpdateReceivedInvoicePaymentStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateReceivedInvoicePaymentStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateReceivedInvoicePaymentStatusCommand(id, request.PaymentStatus, request.Reference);
            var result = await _mediator.Send(command, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating received invoice payment status with ID: {InvoiceId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while updating the received invoice payment status"));
        }
    }

    /// <summary>
    /// Delete an invoice
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    [RequireRole(RoleConstants.ClientAdmin)]
    public async Task<IActionResult> DeleteInvoice(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteInvoiceCommand { InvoiceId = id };
            var result = await _mediator.Send(command, cancellationToken);

            if (result.Success)
            {
                return Success(result, "Invoice deleted successfully");
            }

            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(Error(result.Message));
            }

            return BadRequest(Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice with ID: {InvoiceId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while deleting the invoice"));
        }
    }

    /// <summary>
    /// Get all invoice IRNs (Invoice Reference Numbers)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of invoice IRNs</returns>
    [HttpGet("irns")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetInvoiceIrns(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _mediator.Send(new GetInvoiceIrnsQuery(), cancellationToken);
            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes, result.Irns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice IRNs");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while retrieving invoice IRNs"));
        }
    }

    private bool ValidateInvoice(UploadInvoiceRequest invoice)
    {
        // Add your validation logic here
        return invoice.Party != null
            && !string.IsNullOrWhiteSpace(invoice.Party.Name)
            && invoice.InvoiceItems?.Count > 0;
    }

    // ─── Invoice Drafts ──────────────────────────────────────────────────────

    /// <summary>List all invoice drafts for the current business.</summary>
    [HttpGet("drafts")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> GetInvoiceDrafts(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetInvoiceDraftsQuery(), cancellationToken);
            return Success(result, "Drafts retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice drafts");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while retrieving drafts"));
        }
    }

    /// <summary>Create or update an invoice draft.</summary>
    [HttpPost("drafts")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> SaveInvoiceDraft(
        [FromBody] SaveInvoiceDraftCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving invoice draft");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while saving the draft"));
        }
    }

    /// <summary>Update an existing invoice draft.</summary>
    [HttpPut("drafts/{id:guid}")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> UpdateInvoiceDraft(
        [FromRoute] Guid id,
        [FromBody] SaveInvoiceDraftCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var updateCommand = command with { DraftId = id };
            var result = await _mediator.Send(updateCommand, cancellationToken);
            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice draft {DraftId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while updating the draft"));
        }
    }

    /// <summary>Delete (soft-delete) an invoice draft.</summary>
    [HttpDelete("drafts/{id:guid}")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    public async Task<IActionResult> DeleteInvoiceDraft(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new DeleteInvoiceDraftCommand(id), cancellationToken);
            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice draft {DraftId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while deleting the draft"));
        }
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private static bool IsExcelFile(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return extension is ".xlsx" or ".xls";
    }
}
