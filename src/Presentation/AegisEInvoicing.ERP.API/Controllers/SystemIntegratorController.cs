using EInvoiceIntegrator.Application.Features.SystemIntegrationOperations.Commands.GenerateIrn;
using EInvoiceIntegrator.Application.Features.SystemIntegrationOperations.Commands.GenerateQrCode;
using EInvoiceIntegrator.Application.Features.SystemIntegrationOperations.Commands.ValidateInvoice;
using EInvoiceIntegrator.Domain.Constants;
using EInvoiceIntegrator.Domain.ValueObjects;
using EInvoiceIntegrator.FIRSAccessPoint.Models.Requests.ValidateInvoiceData;
using EInvoiceIntegratorSaas.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EInvoiceIntegratorSaas.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[SwaggerTag("System Integrator Operations")]
[Authorize]
public class SystemIntegratorController(ILogger<SystemIntegratorController> logger) : BaseApiController
{
    private readonly ILogger<SystemIntegratorController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Validate invoice
    /// </summary>
    /// <param name="request">Invoice validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validate invoice</returns>
    [HttpPost("validate-invoice")]
    [SwaggerOperation(
        Summary = "Validate invoice",
        Description = "Validates invoice data to ensure proper FIRS (Federal Inland Revenue Service) compliance before submission. This endpoint checks the invoice structure, required fields, and business rules.",
        OperationId = "ValidateInvoice"
    )]
    [SwaggerResponse(200, "Invoice validated successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Invalid request or validation failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied to business", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Resource not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    public async Task<IActionResult> ValidateInvoice(
        [FromBody] ValidateInvoiceDataRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Invoice validation requested");

            var validateInvoiceCommand = new ValidateInvoiceCommand(request);
            var validateResult = await Mediator.Send(validateInvoiceCommand, cancellationToken);

            return GenericResponse(validateResult.Message,
                validateResult.IsSuccess, validateResult.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during System Integrator invoice validation");

            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred during invoice validation"));
        }
    }

    
    /// <summary>
    /// Generate IRN
    /// </summary>
    /// <param name="request">Generate IRN request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generate IRN</returns>
    [HttpPost("generate-irn")]
    [SwaggerOperation(
        Summary = "Generate IRN (Invoice Reference Number)",
        Description = "Generates a unique Invoice Reference Number (IRN) for FIRS compliance. The IRN is created based on the invoice number and issue date, and is required for all compliant invoices.",
        OperationId = "GenerateIrn"
    )]
    [SwaggerResponse(200, "IRN generated successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Invalid request or validation failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied to business", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Resource not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GenerateIrn(
        [FromBody] GenerateIrnRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generate IRN requested");

            var generateIrnCommand = new GenerateIrnCommand(request.InvoiceNumber, request.IssueDate);
            var generateIrnResult = await Mediator.Send(generateIrnCommand, cancellationToken);

            return GenericResponse(generateIrnResult.Message,
                generateIrnResult.IsSuccess, generateIrnResult.Irn, generateIrnResult.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during System Integrator IRN generation");

            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred during IRN generation"));
        }
    }


    /// <summary>
    /// Generate QRCode
    /// </summary>
    /// <param name="irn">Generate QRCode request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generate QRCode</returns>
    [HttpGet("generate-qrCode/{irn}")]
    [SwaggerOperation(
        Summary = "Generate QR code from IRN",
        Description = "Generates a QR code based on the provided Invoice Reference Number (IRN) for FIRS compliance. The QR code can be embedded in invoice documents for easy verification and scanning.",
        OperationId = "GenerateQrCode"
    )]
    [SwaggerResponse(200, "QR code generated successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Invalid request or validation failed (e.g., invalid IRN format)", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Access denied to business", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Resource not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GenerateQrCode(
        [FromRoute] string irn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generate QrCode requested");

            if (string.IsNullOrEmpty(irn) || !IRN.IsValidIRNFormat(irn))
            {
                return GenericResponse(ResponseMessages.INVALID_IRN_FORMAT,
                    false,statusCode: StatusCodes.Status400BadRequest);
            }

            var generateQrCodeCommand = new GenerateQrCodeCommand(irn);
            var generateQrCodeResult = await Mediator.Send(generateQrCodeCommand, cancellationToken);

            return GenericResponse(generateQrCodeResult.Message,
                generateQrCodeResult.IsSuccess, generateQrCodeResult.QRCode, generateQrCodeResult.StatusCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during System Integrator QrCode generation");

            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred during QrCode generation"));
        }
    }
}
