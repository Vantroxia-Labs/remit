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
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]    [Produces("application/pdf", "application/json")]
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
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]    public async Task<IActionResult> ValidateInvoice(
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
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]    public async Task<IActionResult> ValidateBulkInvoices(
        [FromBody] List<Guid> invoiceIds = null!,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Bulk Invoice validation requested for IDs: {InvoiceId}", string.Join(',', invoiceIds));
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
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]    public async Task<IActionResult> SignInvoice(
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
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]    public async Task<IActionResult> SignBulkInvoices(
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
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]    public async Task<IActionResult> TransmitInvoice(
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
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]    public async Task<IActionResult> TransmitBulkInvoices(
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

                return Success(new
                {
                    result.InvoiceId,
                    result.IRN,
                    result.CurrentStatus,
                    result.Message,
                    result.Pipeline,
                    ExecutionTime = result.Pipeline.TotalExecutionTime
                }, result.Message);
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
    [RequireRole(RoleConstants.ClientAdmin, RoleConstants.ClientUser)]    public async Task<IActionResult> SubmitBulkInvoices(
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
