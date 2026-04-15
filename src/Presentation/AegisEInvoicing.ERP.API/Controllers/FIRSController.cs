using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitBulkInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignBulkInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.TransmitBulkInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.TransmitInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoicePaymentStatus;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateBulkInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.DownloadInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetAllInvoicesForBusiness;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceByIRN;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceStatus;
using AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.DTOs;
using AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Queries.GetReceivedInvoiceById;
using AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Queries.GetReceivedInvoices;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.FIRSAccessPoint.Attributes;
using AegisEInvoicing.FIRSAccessPoint.Interfaces;
using AegisEInvoicing.ERP.API.Extensions;
using AegisEInvoicing.ERP.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AegisEInvoicing.ERP.API.Controllers;

/// <summary>
/// Controller for FIRS (Federal Inland Revenue Service) integration operations.
/// This controller is tenant-agnostic as FIRS integration is shared across all tenants.
/// Access is controlled by role-based authorization (Admin, Accountant, Auditor, Viewer).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[TenantAgnostic("FIRS integration is a shared service used by all tenants")]
public partial class FIRSController(IFIRSHttpClient firsClient, ILogger<FIRSController> logger) : BaseApiController
{
    private readonly IFIRSHttpClient _firsClient = firsClient ?? throw new ArgumentNullException(nameof(firsClient));
    private readonly ILogger<FIRSController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));


    /// <summary>
    /// Creates invoice with party information
    /// </summary>
    /// <param name="request">Invoice creation request with party details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created invoice result</returns>
    [HttpPost("create-invoice")]
    [EnableRateLimiting("InvoiceCreation")] // VAPT: Strict rate limit - 10 invoices per minute per user
    public async Task<IActionResult> CreateInvoice(
        [FromBody] Models.CreateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Invoice creation requested for BusinessId: {BusinessId}", request.AegisBusinessId);

            // Validate the request
            if (request.AegisBusinessId == Guid.Empty)
            {
                _logger.LogWarning("Invoice creation attempted with empty BusinessId");
                return BadRequest(Error("BusinessId is required"));
            }

            if (request.Party == null)
            {
                _logger.LogWarning("Invoice creation attempted without party information");
                return BadRequest(Error("Party information is required"));
            }

            if (request.InvoiceItems == null || !request.InvoiceItems.Any())
            {
                _logger.LogWarning("Invoice creation attempted without invoice items");
                return BadRequest(Error("At least one invoice item is required"));
            }

            // Map the request to command
            var createCommand = request.MapToCreateFIRSInvoiceCommand();
            var createResult = await Mediator.Send(createCommand, cancellationToken);

            if (!createResult.Success)
            {
                _logger.LogWarning("Failed to create invoice for BusinessId: {BusinessId}. Error: {Error}", 
                    request.AegisBusinessId, createResult.Message);
                
                // Handle specific error cases
                if (createResult.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                    createResult.Message.Contains("Invalid BusinessId", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, 
                        Error(createResult.Message));
                }
                
                if (createResult.Message.Contains("not authenticated", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, 
                        Error(createResult.Message));
                }
                
                return BadRequest(Error(createResult.Message));
            }

            _logger.LogInformation("Invoice successfully created with ID: {InvoiceId}, PartyId: {PartyId}, IRN: {IRN}", 
                createResult.InvoiceId, createResult.PartyId, createResult.IRN);

            var responseData = new 
            {
                InvoiceId = createResult.InvoiceId, 
                PartyId = createResult.PartyId,
                IRN = createResult.IRN,
                Message = createResult.Message
            };

            return Success(responseData, string.Empty);
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogError(ex, "Unexpected error during invoice creation for BusinessId: {BusinessId}",
                request?.AegisBusinessId);

            return StatusCode(StatusCodes.Status400BadRequest,
                Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during invoice creation for BusinessId: {BusinessId}", 
                request?.AegisBusinessId);
            
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred during invoice creation"));
        }
    }  

    /// <summary>
    /// Gets comprehensive invoice status information from database
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed invoice status information</returns>
    [HttpGet("invoice-status/{invoiceId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetInvoiceStatus(
        [FromRoute] Guid invoiceId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Invoice status requested for ID: {InvoiceId}", invoiceId);
           var BusinessId= HttpContext.Items["BusinessId"]?.ToString();
            if (string.IsNullOrEmpty(BusinessId) || !Guid.TryParse(BusinessId, out var businessGuid))
            {
                _logger.LogWarning("Get invoice status attempted without valid BusinessId in context");
                return StatusCode(StatusCodes.Status401Unauthorized, Error("User not authenticated or no business associated"));
            }
            var query = new GetInvoiceStatusQuery { InvoiceId = invoiceId, BusinessId= businessGuid };
            var result = await Mediator.Send(query, cancellationToken);
            
           if(!result.IsSuccess)
                return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes);

            _logger.LogInformation("Successfully retrieved invoice status for ID: {InvoiceId}", invoiceId);
            return Success(result.InvoiceStatus, "Invoice status retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving invoice status for ID: {InvoiceId}", invoiceId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred while retrieving invoice status"));
        }
    }

    /// <summary>
    /// Retrieves invoice details by IRN (Invoice Reference Number)
    /// </summary>
    /// <param name="irn">Invoice Reference Number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete invoice details</returns>
    [HttpGet("invoice-by-irn/{irn}")]
    [Authorize]
    public async Task<IActionResult> GetInvoiceByIRN(
        [FromRoute] string irn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(irn))
            {
                _logger.LogWarning("Get invoice by IRN attempted with empty IRN");
                return BadRequest(Error("IRN cannot be empty"));
            }

            _logger.LogInformation("Invoice retrieval requested for IRN: {IRN}", irn);
            var BusinessId = HttpContext.Items["BusinessId"]?.ToString();
            if (string.IsNullOrEmpty(BusinessId) || !Guid.TryParse(BusinessId, out var businessGuid))
            {
                _logger.LogWarning("Get invoice status attempted without valid BusinessId in context");
                return StatusCode(StatusCodes.Status401Unauthorized, Error("User not authenticated or no business associated"));
            }
            var query = new GetInvoiceByIRNQuery { IRN = irn, BusinessId= businessGuid };
            var result = await Mediator.Send(query, cancellationToken);
            
            if (!result.Success)
            {
                _logger.LogWarning("Failed to retrieve invoice for IRN: {IRN}. Error: {Error}", 
                    irn, result.Message);
                
                if (result.Message.Contains("not authenticated", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, Error(result.Message));
                }
                
                if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status404NotFound, Error(result.Message));
                }
                
                return BadRequest(Error(result.Message));
            }
            
            _logger.LogInformation("Successfully retrieved invoice for IRN: {IRN}", irn);
            return Success(result.Invoice, "Invoice retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving invoice for IRN: {IRN}", irn);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred while retrieving the invoice"));
        }
    }

    /// <summary>
    /// Retrieves all invoices for the current user's business with pagination and filtering
    /// </summary>
    /// <param name="status">Optional invoice status filter</param>
    /// <param name="startDate">Optional start date filter (inclusive)</param>
    /// <param name="endDate">Optional end date filter (inclusive)</param>
    /// <param name="searchTerm">Optional search term for IRN, invoice code, notes, or FIRS submission ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="orderBy">Field to order by (irn, issuedate, status, createdat, updatedat)</param>
    /// <param name="orderByDescending">Order in descending order (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of invoices</returns>
    [HttpGet("invoices")]
    [Authorize]
    public async Task<IActionResult> GetAllInvoices(
        [FromQuery] InvoiceStatus? status = null,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? orderBy = null,
        [FromQuery] bool orderByDescending = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1)
            {
                return BadRequest(Error("Page number must be greater than 0"));
            }
            
            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(Error("Page size must be between 1 and 100"));
            }
            
            // Validate date range
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                return BadRequest(Error("Start date cannot be after end date"));
            }

            _logger.LogInformation("All invoices retrieval requested - Page: {PageNumber}, Size: {PageSize}, Status: {Status}, Search: {SearchTerm}",
                pageNumber, pageSize, status?.ToString() ?? "All", searchTerm ?? "None");
            var BusinessId = HttpContext.Items["BusinessId"]?.ToString();
            if (string.IsNullOrEmpty(BusinessId) || !Guid.TryParse(BusinessId, out var businessGuid))
            {
                _logger.LogWarning("Get invoice status attempted without valid BusinessId in context");
                return StatusCode(StatusCodes.Status401Unauthorized, Error("User not authenticated or no business associated"));
            }
            var query = new GetAllInvoicesForBusinessQuery
            {
                InvoiceStatus = status,
                StartDate = startDate,
                EndDate = endDate,
                SearchTerm = searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize,
                OrderBy = orderBy,
                OrderByDescending = orderByDescending,
                 BusinessId= businessGuid
            };
            
            var result = await Mediator.Send(query, cancellationToken);
            
            if (!result.Success)
            {
                _logger.LogWarning("Failed to retrieve invoices. Error: {Error}", result.Message);
                
                if (result.Message.Contains("not authenticated", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, Error(result.Message));
                }
                
                return BadRequest(Error(result.Message));
            }
            
            var invoices = result.Invoices;
            var message = string.IsNullOrWhiteSpace(searchTerm)
                ? $"Retrieved {invoices!.Items.Count} invoices (Page {pageNumber} of {invoices.TotalPages})"
                : $"Found {invoices!.Items.Count} invoices matching '{searchTerm}' (Page {pageNumber} of {invoices.TotalPages})";
            
            // Add pagination headers
            Response.Headers.Append("X-Pagination", System.Text.Json.JsonSerializer.Serialize(new
            {
                invoices.TotalCount,
                invoices.PageSize,
                invoices.PageNumber,
                invoices.TotalPages,
                invoices.HasPreviousPage,
                invoices.HasNextPage
            }));
            
            _logger.LogInformation("Successfully retrieved {Count} invoices", invoices.Items.Count);
            return Success(invoices.Items, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving invoices");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred while retrieving invoices"));
        }
    }

    /// <summary>
    /// Get all received invoices with pagination and filtering
    /// </summary>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="orderBy">Order by field (default: IssueDate)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of received invoices</returns>
    [HttpGet("recieved-invoices")]
    public async Task<IActionResult> GetAllReceivedInvoices(
        [FromQuery] string? searchTerm,
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
                Page = pageNumber,
                PageSize = pageSize,
                SortBy = orderBy
            };

            var result = await Mediator.Send(query, cancellationToken);

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

            var result = await Mediator.Send(query, cancellationToken);

            if (result.Success && result.Invoice != null)
            {
                return Ok(Success(result, "Received invoice retrieved successfully"));
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
    /// Update an existing invoice payment status
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="updateInvoicePaymentStatusRequest">Update command (without ID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Update result</returns>
    [HttpPatch("update-payment-status/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateInvoicePaymentStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateInvoicePaymentStatusRequest updateInvoicePaymentStatusRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateInvoicePaymentStatusCommand(id, updateInvoicePaymentStatusRequest.PaymentStatus);
            var result = await Mediator.Send(command, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, data: null, statusCode: result.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice payment status with ID: {InvoiceId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while updating the invoice payment status"));
        }
    }


    /// <summary>
    ///Validates invoice information
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed invoice status information</returns>
    [HttpPost("Validate/{invoiceId:guid}")]
    [EnableRateLimiting("InvoiceOperations")] // VAPT: Rate limit - 20 operations per minute per user
    public async Task<IActionResult> ValidateInvoice(
        [FromRoute] Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Invoice validation requested for ID: {InvoiceId}", invoiceId);
            var query = new ValidateInvoiceCommand(invoiceId);
            var result = await Mediator.Send(query, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, null, result.StatusCodes);
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating invoice for ID: {InvoiceId}", invoiceId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred while validating invoice"));
        }
    }

    /// <summary>
    ///Bulk Validate invoices
    /// </summary>
    /// <param name="invoiceIds">Invoice IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed invoice status information</returns>
    [HttpPost("validatebulk")]
    [EnableRateLimiting("BulkOperations")] // VAPT: Strict rate limit - 5 bulk operations per 5 minutes per user
    public async Task<IActionResult> ValidateBulkInvoices(
        [FromBody] List<Guid> invoiceIds = null!,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Bulk Invoice validation requested for IDs: {InvoiceId}", string.Join(',', invoiceIds));
            var query = new ValidateBulkInvoiceCommand(invoiceIds);
            var result = await Mediator.Send(query, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, null, result.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating bulk invoice for IDs: {InvoiceId}", string.Join(',', invoiceIds));
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred while validating bulk invoices"));
        }
    }


    /// <summary>
    ///Downloads invoice information
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed invoice status information</returns>
    [HttpGet("download/{invoiceId:guid}")]
    public async Task<IActionResult> DownloadInvoice(
        [FromRoute] Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Invoice download requested for ID: {InvoiceId}", invoiceId);
            var query = new DownloadInvoiceQuery(invoiceId);
            var result = await Mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return GenericResponse(result.Message, result.IsSuccess, null, result.StatusCodes);
            }

            // Return PDF file
            if (!string.IsNullOrEmpty(result.InvoiceData))
            {
                return GenerateInvoiceFile(result.InvoiceData, result.QrCode!);
            }

            return Error("Invoice data is empty", StatusCodes.Status404NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error downloading invoice for ID: {InvoiceId}", invoiceId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred while downloading invoice"));
        }
    }

    /// <summary>
    /// Signs invoice
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed invoice status information</returns>
    [HttpPost("Sign/{invoiceId:guid}")]
    [EnableRateLimiting("InvoiceOperations")] // VAPT: Rate limit - 20 operations per minute per user
    public async Task<IActionResult> SignInvoice(
        [FromRoute] Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Invoice signing requested for ID: {InvoiceId}", invoiceId);
            var query = new SignInvoiceCommand(invoiceId);
            var result = await Mediator.Send(query, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, null, result.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error signing invoice for ID: {InvoiceId}", invoiceId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred while signing invoice"));
        }
    }


    /// <summary>
    /// Bulk Signs invoices
    /// </summary>
    /// <param name="invoiceIds">Invoice IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed invoice status information</returns>
    [HttpPost("signbulk")]
    [EnableRateLimiting("BulkOperations")] // VAPT: Strict rate limit - 5 bulk operations per 5 minutes per user
    public async Task<IActionResult> SignBulkInvoices(
        [FromBody] List<Guid> invoiceIds = null!,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Bulk Invoice signing requested for IDs: {InvoiceId}", string.Join(',', invoiceIds));
            var query = new SignBulkInvoiceCommand(invoiceIds);
            var result = await Mediator.Send(query, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, null, result.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error signing bulk invoices for IDs: {InvoiceId}", string.Join(',', invoiceIds));
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred while signing bulk invoice"));
        }
    }


    /// <summary>
    /// Transmits invoice
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed invoice status information</returns>
    [HttpPost("transmit/{invoiceId:guid}")]
    [EnableRateLimiting("InvoiceOperations")] // VAPT: Rate limit - 20 operations per minute per user
    public async Task<IActionResult> TransmitInvoice(
        [FromRoute] Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Invoice transmission requested for ID: {InvoiceId}", invoiceId);
            var query = new TransmitInvoiceCommand(invoiceId);
            var result = await Mediator.Send(query, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, null, result.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error transmitting invoice for ID: {InvoiceId}", invoiceId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred while transmitting invoice"));
        }
    }

    /// <summary>
    /// Bulk Transmit invoices
    /// </summary>
    /// <param name="invoiceIds">Invoice IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed invoice status information</returns>
    [HttpPost("transmitbulk")]
    [EnableRateLimiting("BulkOperations")] // VAPT: Strict rate limit - 5 bulk operations per 5 minutes per user
    public async Task<IActionResult> TransmitBulkInvoices(
        [FromBody] List<Guid> invoiceIds = null!,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Bulk Invoice transmission requested for IDs: {InvoiceId}", string.Join(',', invoiceIds));
            var query = new TransmitBulkInvoiceCommand(invoiceIds);
            var result = await Mediator.Send(query, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, null, result.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error transmitting bulk invoices for IDs: {InvoiceId}", string.Join(',', invoiceIds));
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred while transmitting bulk invoice"));
        }
    }

    /// <summary>
    /// Creates and submits an invoice through the complete pipeline (Create ? Validate ? Sign ? Transmit)
    /// </summary>
    /// <param name="request">Invoice creation request with party details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete pipeline execution result</returns>
    [HttpPost("create-and-submit-invoice")]
    [EnableRateLimiting("ConsolidatedInvoiceSubmission")]
    public async Task<IActionResult> CreateAndSubmitInvoice(
        [FromBody] Models.CreateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Consolidated invoice pipeline requested for BusinessId: {BusinessId}", 
                request.AegisBusinessId);

            // Validate the request
            if (request.AegisBusinessId == Guid.Empty)
            {
                _logger.LogWarning("Invoice creation attempted with empty BusinessId");
                return BadRequest(Error("BusinessId is required"));
            }

            if (request.Party == null)
            {
                _logger.LogWarning("Invoice creation attempted without party information");
                return BadRequest(Error("Party information is required"));
            }

            if (request.InvoiceItems == null || !request.InvoiceItems.Any())
            {
                _logger.LogWarning("Invoice creation attempted without invoice items");
                return BadRequest(Error("At least one invoice item is required"));
            }

            // Map request to CreateFIRSInvoiceCommand
            var createCommand = request.MapToCreateFIRSInvoiceCommand();

            // Create the consolidated command
            var consolidatedCommand = new CreateAndSubmitInvoiceCommand
            {
                InvoiceData = createCommand
            };

            // Execute the pipeline
            var result = await Mediator.Send(consolidatedCommand, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Consolidated pipeline completed successfully. InvoiceId: {InvoiceId}, IRN: {IRN}, Status: {Status}",
                    result.InvoiceId, result.IRN, result.CurrentStatus);

                return Ok(Success(new
                {
                    result.InvoiceId,
                    result.IRN,
                    result.CurrentStatus,
                    result.Message,
                    result.Pipeline,
                    ExecutionTime = result.Pipeline.TotalExecutionTime
                }, result.Message));
            }
            else
            {
                _logger.LogWarning(
                    "Consolidated pipeline completed with errors. InvoiceId: {InvoiceId}, FailedAt: {FailedAt}, Status: {Status}",
                    result.InvoiceId, result.FailedAt, result.CurrentStatus);

                // Return appropriate status code based on result
                return StatusCode(result.StatusCodes, new
                {
                    success = false,
                    result.InvoiceId,
                    result.IRN,
                    result.CurrentStatus,
                    result.Message,
                    result.FailedAt,
                    result.Pipeline,
                    result.ErrorDetails,
                    ExecutionTime = result.Pipeline.TotalExecutionTime
                });
            }
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogError(ex, 
                "Validation error during consolidated invoice pipeline for BusinessId: {BusinessId}",
                request?.AegisBusinessId);

            return StatusCode(StatusCodes.Status400BadRequest, Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Unexpected error during consolidated invoice pipeline for BusinessId: {BusinessId}",
                request?.AegisBusinessId);

            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred during invoice submission pipeline"));
        }
    }

    /// <summary>
    /// Creates and submits multiple invoices through the complete pipeline in bulk
    /// </summary>
    /// <param name="request">List of invoices to create and submit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk processing results with individual invoice statuses</returns>
    [HttpPost("create-and-submit-invoices-bulk")]
    [EnableRateLimiting("ConsolidatedBulkSubmission")]
    public async Task<IActionResult> CreateAndSubmitBulkInvoices(
        [FromBody] List<Models.CreateInvoiceRequest> request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Bulk consolidated invoice pipeline requested for {Count} invoices",
                request.Count);

            if (request == null || request.Count == 0)
            {
                return BadRequest(Error("At least one invoice is required for bulk processing"));
            }

            if (request.Count > 100)
            {
                return BadRequest(Error("Bulk processing is limited to 100 invoices per request"));
            }

            // Map all requests to CreateFIRSInvoiceCommand
            var createCommands = request
                .Select(r => r.MapToCreateFIRSInvoiceCommand())
                .ToList();

            // Create bulk command
            var bulkCommand = new CreateAndSubmitBulkInvoiceCommand
            {
                Invoices = createCommands
            };

            // Execute bulk pipeline
            var result = await Mediator.Send(bulkCommand, cancellationToken);

            _logger.LogInformation(
                "Bulk consolidated pipeline completed. Total: {Total}, Success: {Success}, Failed: {Failed}, Time: {Time}ms",
                result.TotalProcessed,
                result.SuccessCount,
                result.FailedCount,
                result.TotalExecutionTime?.TotalMilliseconds ?? 0);

            // Return results
            return StatusCode(result.StatusCodes, new
            {
                result.Success,
                result.TotalProcessed,
                result.SuccessCount,
                result.FailedCount,
                result.Message,
                result.Results,
                result.Errors,
                ExecutionTime = result.TotalExecutionTime
            });
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogError(ex, "Validation error during bulk consolidated invoice pipeline");
            return StatusCode(StatusCodes.Status400BadRequest, Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during bulk consolidated invoice pipeline");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred during bulk invoice submission pipeline"));
        }
    }
}
