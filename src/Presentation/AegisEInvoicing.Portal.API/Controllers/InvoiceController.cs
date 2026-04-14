using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.Invoice.Request;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Common.Models.InvoiceData;
using AegisEInvoicing.Application.Extensions;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.DeleteInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoicePaymentStatus;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UploadInvoices;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetAllInvoices;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceById;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceIrns;
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
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Controller for invoice management operations
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[SwaggerTag("Invoice Management Operations - Create, Read, Update, Delete invoices")]
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
    [SwaggerOperation(
        Summary = "Create Invoice",
        Description = @"Creates a new invoice with invoice items compliant with FIRS UBL (Universal Business Language) format.

**Rate Limit:** Maximum 10 invoices per user per minute to prevent flooding attacks.

**Features:**
- FIRS UBL 2.1 compliant invoice structure
- Automatic IRN (Invoice Reference Number) generation
- VAT calculation and tax category assignment
- Support for multiple invoice items
- Party (customer) validation
- Payment terms and delivery period tracking

**Access Control:**
- **Required Roles**: Business Administrator or Business User
- **Tenant Isolation**: Invoice automatically assigned to authenticated business

**Validation Rules:**
- Party ID must exist and belong to business
- At least one invoice item is required
- Invoice type must be valid FIRS type
- Currency must be supported (NGN, USD, EUR, GBP)
- Tax category must be valid
- Unit prices must be positive
- Due date must be after issue date

**Invoice Status Workflow:**
1. **Draft** - Initial state, can be edited
2. **Validated** - Passed FIRS validation
3. **Signed** - Digitally signed for FIRS
4. **Submitted** - Sent to FIRS
5. **Confirmed** - Confirmed by FIRS

**Example Request:**
```json
{
  ""partyId"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
  ""issueDate"": ""2025-01-15"",
  ""dueDate"": ""2025-02-15"",
  ""invoiceType"": {
    ""name"": ""Standard Invoice"",
    ""code"": ""380""
  },
  ""currency"": {
    ""name"": ""Nigerian Naira"",
    ""code"": ""NGN""
  },
  ""deliveryPeriod"": {
    ""startDate"": ""2025-01-01"",
    ""endDate"": ""2025-01-31""
  },
  ""paymentMeans"": {
    ""code"": ""30"",
    ""name"": ""Credit Transfer""
  },
  ""paymentReference"": ""PAY-2025-001"",
  ""paymentTerms"": ""Net 30 days"",
  ""note"": ""Payment due within 30 days"",
  ""invoiceItems"": [
    {
      ""businessItemId"": ""7c8e9f10-1234-5678-9abc-def012345678"",
      ""quantity"": 10,
      ""unitPrice"": 50000.00,
      ""discount"": 5000.00
    }
  ]
}
```

**Example Response:**
```json
{
  ""data"": {
    ""invoiceId"": ""9d7e6f5c-4321-8765-bcde-f012345678ab"",
    ""irn"": ""ITW001-2025-00001"",
    ""status"": ""Draft"",
    ""totalAmount"": 495000.00,
    ""vatAmount"": 37125.00,
    ""message"": ""Invoice created successfully""
  },
  ""message"": ""Invoice created successfully"",
  ""isSuccess"": true,
  ""statusCode"": 201
}
```"
    )]
    [SwaggerResponse(201, "Invoice created successfully", typeof(ApiResponse<CreateInvoiceResult>))]
    [SwaggerResponse(400, "Invalid request or validation failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Party or business item not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
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
            return BadRequest();
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
    [SwaggerOperation(
        Summary = "Bulk Upload Invoices",
        Description = @"Bulk creates invoices from an Excel file upload with streaming processing for large files.

**File Requirements:**
- **Format**: Excel (.xlsx or .xls)
- **Max Size**: 500 MB
- **Batch Processing**: Processed in batches of 1000 invoices
- **Required Columns**: PartyName, PartyTIN, IssueDate, DueDate, InvoiceType, Currency, ItemName, Quantity, UnitPrice
- **Optional Columns**: PaymentReference, Note, PaymentTerms, Discount, TaxAmount

**Streaming Processing:**
- Large files are processed in chunks to avoid memory issues
- Progress is logged during conversion
- Failed rows are tracked with error messages

**Validation:**
- Each invoice row is validated independently
- Invalid invoices are skipped and reported
- Valid invoices are created successfully

**Excel Template:**
| PartyName | PartyTIN | IssueDate | DueDate | InvoiceType | Currency | ItemName | Quantity | UnitPrice | Discount |
|-----------|----------|-----------|---------|-------------|----------|----------|----------|-----------|----------|
| ABC Corp | 12345678 | 2025-01-15 | 2025-02-15 | 380 | NGN | Consulting | 10 | 50000 | 5000 |

**Example Response:**
```json
{
  ""data"": {
    ""totalProcessed"": 1000,
    ""successfullyCreated"": 950,
    ""failed"": 50,
    ""errors"": [
      {
        ""row"": 12,
        ""partyName"": ""XYZ Ltd"",
        ""error"": ""Party not found""
      },
      {
        ""row"": 45,
        ""partyName"": ""Invalid Corp"",
        ""error"": ""Invalid TIN format""
      }
    ],
    ""processingTime"": ""45.23s""
  },
  ""message"": ""Bulk upload completed: 950 created, 50 failed"",
  ""isSuccess"": true,
  ""statusCode"": 200
}
```"
    )]
    [SwaggerResponse(200, "Upload processed (check response for individual invoice results)", typeof(ApiResponse<UploadInvoiceResult>))]
    [SwaggerResponse(400, "Invalid file format or empty file", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(413, "File size exceeds 500MB limit", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error or file processing error", typeof(ApiResponse<object>))]
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
    [SwaggerOperation(
        Summary = "Get Invoice by ID",
        Description = @"Retrieves complete invoice details including all invoice items, party information, and FIRS status.

**Returned Information:**
- Invoice header (IRN, dates, totals, status)
- All invoice line items with quantities and pricing
- Party (customer) details
- Payment information and terms
- Tax calculations and VAT breakdown
- FIRS submission status and timestamps

**Access Control:**
- **Aegis Admin**: Can view all invoices across all businesses
- **Business Admin/User**: Can only view invoices from their own business
- **Tenant Isolation**: Enforced automatically

**Example Response:**
```json
{
  ""data"": {
    ""invoice"": {
      ""invoiceId"": ""9d7e6f5c-4321-8765-bcde-f012345678ab"",
      ""irn"": ""ITW001-2025-00001"",
      ""issueDate"": ""2025-01-15"",
      ""dueDate"": ""2025-02-15"",
      ""status"": ""Confirmed"",
      ""paymentStatus"": ""Paid"",
      ""totalAmount"": 495000.00,
      ""vatAmount"": 37125.00,
      ""party"": {
        ""name"": ""ABC Corporation"",
        ""tin"": ""12345678""
      },
      ""invoiceItems"": [
        {
          ""itemName"": ""Consulting Services"",
          ""quantity"": 10,
          ""unitPrice"": 50000.00,
          ""discount"": 5000.00,
          ""lineTotal"": 495000.00
        }
      ]
    }
  },
  ""message"": ""Invoice retrieved successfully"",
  ""isSuccess"": true,
  ""statusCode"": 200
}
```"
    )]
    [SwaggerResponse(200, "Invoice found and retrieved successfully", typeof(ApiResponse<GetInvoiceByIdResult>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Insufficient permissions or accessing invoice from different business", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
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
    [SwaggerOperation(
        Summary = "Get all invoices",
        Description = "Retrieves a paginated list of invoices with optional filtering"
    )]
    [SwaggerResponse(200, "Invoices retrieved successfully", typeof(ApiResponse<PaginatedList<InvoiceDto>>))]
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
            return Ok(Success(result, "Invoices retrieved successfully"));
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
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="orderBy">Order by field (default: IssueDate)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of received invoices</returns>
    [HttpGet("recieved-invoices")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Get all received invoices",
        Description = "Retrieves a paginated list of received invoices with optional filtering"
    )]
    [SwaggerResponse(200, "Invoices retrieved successfully", typeof(ApiResponse<PaginatedList<InvoiceDto>>))]
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
    [SwaggerOperation(
        Summary = "Get received invoice by ID",
        Description = @"Retrieves complete details for a single received invoice including all line items, tax totals, and party information.

**Returned Information:**
- Invoice header (IRN, dates, totals, status)
- Supplier and customer details with full addresses
- All financial amounts and tax calculations
- Invoice lines (JSON format)
- Tax totals breakdown (JSON format)
- Reconciliation status and history
- Complete audit trail

**Access Control:**
- **Aegis Admin**: Can view all received invoices across all businesses
- **Business Admin/User**: Can only view received invoices for their own business
- **Tenant Isolation**: Enforced automatically

**Example Response:**
```json
{
  ""data"": {
    ""invoice"": {
      ""id"": ""9d7e6f5c-4321-8765-bcde-f012345678ab"",
      ""irn"": ""ABC123456789"",
      ""issueDate"": ""2025-01-15"",
      ""dueDate"": ""2025-02-15"",
      ""paymentStatus"": ""Unpaid"",
      ""entryStatus"": ""Valid"",
      ""supplierPartyName"": ""ABC Suppliers Ltd"",
      ""supplierTIN"": ""12345678"",
      ""customerPartyName"": ""My Business"",
      ""customerTIN"": ""87654321"",
      ""payableAmount"": 495000.00,
      ""invoiceLinesJson"": ""[...]"",
      ""taxTotalJson"": ""[...]"",
      ""isReconciled"": false
    }
  },
  ""message"": ""Received invoice retrieved successfully"",
  ""isSuccess"": true,
  ""statusCode"": 200
}
```"
    )]
    [SwaggerResponse(200, "Received invoice found and retrieved successfully", typeof(ApiResponse<GetReceivedInvoiceByIdResult>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Insufficient permissions or accessing invoice from different business", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Received invoice not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
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
    [SwaggerOperation(
        Summary = "Export invoices to Excel",
        Description = "Exports invoices with full internal data to Excel format. Invoice items are grouped by PaymentReference."
    )]
    [SwaggerResponse(200, "Excel file generated successfully", typeof(FileContentResult))]
    [SwaggerResponse(404, "No invoices found matching criteria", typeof(ApiResponse<object>))]
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
    [SwaggerOperation(
        Summary = "Update invoice",
        Description = "Updates an existing invoice (only draft invoices can be updated)"
    )]
    [SwaggerResponse(200, "Invoice updated successfully", typeof(ApiResponse<UpdateInvoiceResult>))]
    [SwaggerResponse(400, "Invalid request or invoice cannot be updated", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
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
                return Ok(Success(result, "Invoice updated successfully"));
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
    [SwaggerOperation(
        Summary = "Update invoice payment status",
        Description = "Update an existing invoice payment status"
    )]
    [SwaggerResponse(200, "Invoice updated successfully", typeof(ApiResponse<UpdateInvoicePaymentStatusResult>))]
    [SwaggerResponse(400, "Invalid request or invoice cannot be updated", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
    public async Task<IActionResult> UpdateInvoicePaymentStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateInvoicePaymentStatusRequest updateInvoicePaymentStatusRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateInvoicePaymentStatusCommand(id, updateInvoicePaymentStatusRequest.PaymentStatus);
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
    /// Delete an invoice
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    [RequireRole(RoleConstants.ClientAdmin)]
    [SwaggerOperation(
        Summary = "Delete invoice",
        Description = "Deletes an invoice (only draft invoices can be deleted)"
    )]
    [SwaggerResponse(200, "Invoice deleted successfully", typeof(ApiResponse<DeleteInvoiceResult>))]
    [SwaggerResponse(400, "Invoice cannot be deleted", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
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
                return Ok(Success(result, "Invoice deleted successfully"));
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
    [SwaggerOperation(
        Summary = "Get Invoice IRNs",
        Description = "Retrieves all invoice IRNs (Invoice Reference Numbers) for the authenticated user's business or specified business (admins only)"
    )]
    [SwaggerResponse(200, "IRNs retrieved successfully", typeof(ApiResponse<GetInvoiceIrnsResult>))]
    [SwaggerResponse(400, "Invalid request", typeof(ApiResponse<object>))]
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

    private static bool IsExcelFile(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return extension is ".xlsx" or ".xls";
    }
}