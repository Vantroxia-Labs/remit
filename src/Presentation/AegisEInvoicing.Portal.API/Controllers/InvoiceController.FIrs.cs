using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignBulkInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SubmitExistingInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SubmitExistingInvoicesBulk;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.TransmitBulkInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.TransmitInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateBulkInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.Queries.DownloadInvoice;
using AegisEInvoicing.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace AegisEInvoicing.Portal.API.Controllers;

public partial class InvoiceController
{
    /// <summary>
    ///Downloads invoice information
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed invoice status information</returns>
    [HttpGet("download/{invoiceId:guid}")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Download invoice",
        Description = "Downloads Invoice as PDF"
    )]
    [SwaggerResponse(200, "Invoice downloaded successfully as PDF", typeof(FileResult))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied to this business", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    [Produces("application/pdf", "application/json")]
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
                return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes);
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
    ///Validates invoice information
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed invoice status information</returns>
    [HttpPost("Validate/{invoiceId:guid}")]
    [EnableRateLimiting("InvoiceOperations")] // Rate limit: 20 operations per minute per user
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Validate invoice",
        Description = "Validate Invoice. Rate limited to 20 operations per minute per user."
    )]
    [SwaggerResponse(200, "Invoice validation successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied to this business", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    public async Task<IActionResult> ValidateInvoice(
        [FromRoute] Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Invoice validation requested for ID: {InvoiceId}", invoiceId);
            var query = new ValidateInvoiceCommand(invoiceId);
            var result = await Mediator.Send(query, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes);
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
    [EnableRateLimiting("BulkOperations")] // Strict rate limit: 5 bulk operations per 5 minutes per user
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Bulk Validate invoices",
        Description = "Bulk Validate Invoices. Rate limited to 5 bulk operations per 5 minutes per user to prevent system overload."
    )]
    [SwaggerResponse(101, "Bulk Invoice validation Processed", typeof(ApiResponse<object>))]
    [SwaggerResponse(200, "Bulk Invoice validation successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied to this business", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    public async Task<IActionResult> ValidateBulkInvoices(
        [FromBody] List<Guid> invoiceIds = null!,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Bulk Invoice validation requested for IDs: {InvoiceId}", string.Join(',',invoiceIds));
            var query = new ValidateBulkInvoiceCommand(invoiceIds);
            var result = await Mediator.Send(query, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating bulk invoice for IDs: {InvoiceId}", string.Join(',', invoiceIds));
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred while validating bulk invoices"));
        }
    }

    /// <summary>
    /// Signs invoice
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed invoice status information</returns>
    [HttpPost("Sign/{invoiceId:guid}")]
    [EnableRateLimiting("InvoiceOperations")] // Rate limit: 20 operations per minute per user
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Sign invoice",
        Description = "Sign Invoice. Rate limited to 20 operations per minute per user."
    )]
    [SwaggerResponse(200, "Invoice signing successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied to this business", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    public async Task<IActionResult> SignInvoice(
        [FromRoute] Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Invoice signing requested for ID: {InvoiceId}", invoiceId);
            var query = new SignInvoiceCommand(invoiceId);
            var result = await Mediator.Send(query, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes);
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
    [EnableRateLimiting("BulkOperations")] // Strict rate limit: 5 bulk operations per 5 minutes per user
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Bulk Sign invoices",
        Description = "Bulk Sign Invoices. Rate limited to 5 bulk operations per 5 minutes per user to prevent system overload."
    )]
    [SwaggerResponse(200, "Invoice signing successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied to this business", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    public async Task<IActionResult> SignBulkInvoices(
        [FromBody] List<Guid> invoiceIds = null!,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Bulk Invoice signing requested for IDs: {InvoiceId}", string.Join(',', invoiceIds));
            var query = new SignBulkInvoiceCommand(invoiceIds);
            var result = await Mediator.Send(query, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes, result);
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
    [EnableRateLimiting("InvoiceOperations")] // Rate limit: 20 operations per minute per user
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Transmit invoice",
        Description = "Transmit Invoice. Rate limited to 20 operations per minute per user."
    )]
    [SwaggerResponse(200, "Invoice transmitted successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied to this business", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    public async Task<IActionResult> TransmitInvoice(
        [FromRoute] Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Invoice transmission requested for ID: {InvoiceId}", invoiceId);
            var query = new TransmitInvoiceCommand(invoiceId);
            var result = await Mediator.Send(query, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes);
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
    [EnableRateLimiting("BulkOperations")] // Strict rate limit: 5 bulk operations per 5 minutes per user
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Bulk Transmit invoices",
        Description = "Bulk Transmit Invoices. Rate limited to 5 bulk operations per 5 minutes per user to prevent system overload."
    )]
    [SwaggerResponse(200, "Invoice transmission successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied to this business", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    public async Task<IActionResult> TransmitBulkInvoices(
        [FromBody] List<Guid> invoiceIds = null!,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Bulk Invoice transmission requested for IDs: {InvoiceId}", string.Join(',', invoiceIds));
            var query = new TransmitBulkInvoiceCommand(invoiceIds);
            var result = await Mediator.Send(query, cancellationToken);

            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error transmitting bulk invoices for IDs: {InvoiceId}", string.Join(',', invoiceIds));
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An error occurred while transmitting bulk invoice"));
        }
    }

    /// <summary>
    /// Validates, signs, and transmits an existing invoice through the complete pipeline
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete pipeline execution result</returns>
    [HttpPost("submit-invoice/{invoiceId:guid}")]
    [EnableRateLimiting("InvoiceOperations")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Submit Invoice (Validate → Sign → Transmit)",
        Description = @"Submits an existing invoice through the complete FIRS compliance pipeline:
**Pipeline Steps (Always Executed):**
1. **VALIDATE** - Validates invoice against FIRS requirements
2. **SIGN** - Digitally signs invoice via NRS (Interswitch)  
3. **TRANSMIT** - Transmits signed invoice to FIRS

**Key Features:**
- Single endpoint replaces 3 separate API calls
- All steps always execute (no skipping)
- Continues processing even if intermediate steps fail
- Returns detailed status for each pipeline step
- Final invoice status reflects last successful step

**Note:** Invoice must already exist (created separately). Use this for invoices created via the regular CREATE endpoint.

**Rate Limit:** 20 operations per minute per user

**Access Control:**
- **Required Roles**: Business User or Administrator
- **Tenant Isolation**: Automatically enforced via authentication

**Example Success Response:**
```json
{
  ""success"": true,
  ""invoiceId"": ""guid"",
  ""irn"": ""ITW00000001-E9E0C0D3-20250115"",
  ""currentStatus"": ""TRANSMITTED"",
  ""message"": ""Invoice submitted successfully"",
  ""pipeline"": {
    ""validate"": { ""status"": ""SUCCESS"", ""message"": ""Invoice validated"" },
    ""sign"": { ""status"": ""SUCCESS"", ""message"": ""Invoice signed"" },
    ""transmit"": { ""status"": ""SUCCESS"", ""message"": ""Invoice transmitted"" }
  }
}
```",
        OperationId = "SubmitInvoice",
        Tags = new[] { "Invoice Management Operations" }
    )]
    [SwaggerResponse(200, "Invoice successfully processed through entire pipeline", typeof(ApiResponse<object>))]
    [SwaggerResponse(207, "Invoice processed but some pipeline steps failed (Multi-Status)", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Invoice not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    public async Task<IActionResult> SubmitInvoice(
        [FromRoute] Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Invoice submission pipeline requested for InvoiceId: {InvoiceId}",
                invoiceId);

            // Create a submission command that validates, signs, and transmits
            var command = new SubmitExistingInvoiceCommand
            {
                InvoiceId = invoiceId
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Submission pipeline completed successfully. InvoiceId: {InvoiceId}, Status: {Status}",
                    invoiceId, result.CurrentStatus);

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
                    "Submission pipeline completed with errors. InvoiceId: {InvoiceId}, FailedAt: {FailedAt}, Status: {Status}",
                    invoiceId, result.FailedAt, result.CurrentStatus);

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
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during submission pipeline for InvoiceId: {InvoiceId}",
                invoiceId);

            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred during invoice submission pipeline"));
        }
    }

    /// <summary>
    /// Submits multiple existing invoices through the complete pipeline in bulk
    /// </summary>
    /// <param name="invoiceIds">List of invoice IDs to submit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk processing results with individual invoice statuses</returns>
    [HttpPost("submit-invoices-bulk")]
    [EnableRateLimiting("BulkOperations")]
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]
    [SwaggerOperation(
        Summary = "Bulk Submit Invoices (Validate → Sign → Transmit)",
        Description = @"Submits multiple existing invoices through the complete FIRS compliance pipeline.
Each invoice goes through: Validate → Sign → Transmit

**Features:**
- Process up to 100 invoices per request
- Each invoice processed independently
- Continues processing even if individual invoices fail
- Returns detailed results for each invoice
- Summary includes success/failure counts

**Rate Limit:** 5 bulk operations per 5 minutes per user

**Processing Behavior:**
- Invoices processed sequentially (not parallel)
- Each invoice gets its own transaction
- Failure of one invoice doesn't affect others
- Progress logged every 10 invoices

**Example Response:**
```json
{
  ""success"": false,
  ""totalProcessed"": 100,
  ""successCount"": 95,
  ""failedCount"": 5,
  ""message"": ""Bulk processing completed: 95 succeeded, 5 failed"",
  ""results"": [ /* individual invoice results */ ],
  ""errors"": [
    {
      ""invoiceIndex"": 12,
      ""invoiceId"": ""guid"",
      ""errorMessage"": ""Validation failed"",
      ""failedAt"": ""validate""
    }
  ]
}
```",
        OperationId = "SubmitBulkInvoices",
        Tags = new[] { "Invoice Management Operations" }
    )]
    [SwaggerResponse(200, "All invoices processed successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(207, "Bulk processing completed with some failures (Multi-Status)", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Invalid request or no invoices provided", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    public async Task<IActionResult> SubmitBulkInvoices(
        [FromBody] List<Guid> invoiceIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Bulk submission pipeline requested for {Count} invoices",
                invoiceIds.Count);

            if (invoiceIds == null || invoiceIds.Count == 0)
            {
                return BadRequest(Error("At least one invoice ID is required for bulk processing"));
            }

            if (invoiceIds.Count > 100)
            {
                return BadRequest(Error("Bulk processing is limited to 100 invoices per request"));
            }

            var command = new SubmitExistingInvoicesBulkCommand
            {
                InvoiceIds = invoiceIds
            };

            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation(
                "Bulk submission pipeline completed. Total: {Total}, Success: {Success}, Failed: {Failed}",
                result.TotalProcessed,
                result.SuccessCount,
                result.FailedCount);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during bulk submission pipeline");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred during bulk invoice submission pipeline"));
        }
    }
}
