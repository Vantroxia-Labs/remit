using EInvoiceIntegrator.Application.Features.SystemIntegrationOperations.Commands.GenerateIrn;
using EInvoiceIntegrator.Application.Features.SystemIntegrationOperations.Commands.GenerateQrCode;
using EInvoiceIntegrator.Application.Features.SystemIntegrationOperations.Commands.ValidateInvoice;
using EInvoiceIntegrator.Domain.Constants;
using EInvoiceIntegrator.Domain.ValueObjects;
using EInvoiceIntegrator.FIRSAccessPoint.Models.Requests.ValidateInvoiceData;
using EInvoiceIntegratorSaas.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EInvoiceIntegratorSaas.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
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
