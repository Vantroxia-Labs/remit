using Asp.Versioning;
using AegisEInvoicing.Application.Features.TinValidation;
using AegisEInvoicing.FIRSAccessPoint.Attributes;
using AegisEInvoicing.ERP.API.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.ERP.API.Controllers;

/// <summary>
/// TIN Validation endpoints for validating Tax Identification Numbers
/// and checking MBS enrollment status
/// </summary>
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[TenantAgnostic("TIN validation is a shared service used by all tenants")]
[ApiVersion("1.0")]
public class TinValidationController(ISender sender, ILogger<TinValidationController> logger) : BaseApiController
{
    private readonly ISender _sender = sender;
    private readonly ILogger<TinValidationController> _logger = logger;

    /// <summary>
    /// Validate TIN and check MBS enrollment status
    /// </summary>
    /// <param name="request">TIN validation request containing the TIN to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>TIN validation result with enrollment status</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ApiResponse<TinValidationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateTin(
        [FromBody] ValidateTinRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Tin))
            {
                _logger.LogWarning("TIN validation attempted with empty TIN");
                return Error("TIN cannot be empty");
            }

            _logger.LogInformation("TIN validation requested for TIN: {Tin}", MaskTin(request.Tin));

            var query = new ValidateTinQuery(request.Tin);
            var result = await _sender.Send(query, cancellationToken);

            var response = new TinValidationResponse
            {
                Status = result.Status.ToString(),
                IsValid = result.Success,
                IsEnrolled = result.Status == TinValidationStatus.ValidAndEnrolled,
                Message = result.Message,
                BusinessName = result.BusinessName,
                BusinessReference = result.BusinessReference,
                AppReference = result.AppReference,
                HasWebhookSetup = result.HasWebhookSetup
            };

            if (result.Success)
            {
                _logger.LogInformation("TIN validation successful for TIN: {Tin}", MaskTin(request.Tin));
                return Success(response, result.Message);
            }
            else
            {
                _logger.LogWarning("TIN validation failed for TIN: {Tin}. Reason: {Reason}",
                    MaskTin(request.Tin), result.Message);
                return Error(result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during TIN validation");
            return Error("An unexpected error occurred during TIN validation", StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Mask TIN for logging (show only last 4 digits)
    /// </summary>
    private static string MaskTin(string tin)
    {
        if (string.IsNullOrWhiteSpace(tin) || tin.Length < 4)
            return "****";

        return $"***********{tin[^4..]}";
    }
}

/// <summary>
/// TIN validation request DTO
/// </summary>
public sealed record ValidateTinRequest
{
    /// <summary>
    /// Tax Identification Number to validate (12 digits)
    /// </summary>
    /// <example>123456789012</example>
    public string Tin { get; init; } = string.Empty;
}

/// <summary>
/// TIN validation response DTO
/// </summary>
public sealed record TinValidationResponse
{
    /// <summary>
    /// Validation status: ValidAndEnrolled, InvalidOrNotEnrolled, or Error
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Indicates if the TIN is valid
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Indicates if the buyer is enrolled on the MBS portal
    /// Note: This is false for both invalid TINs and valid TINs not enrolled
    /// </summary>
    public bool IsEnrolled { get; init; }

    /// <summary>
    /// Validation message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Business name (if enrolled)
    /// </summary>
    public string? BusinessName { get; init; }

    /// <summary>
    /// Business reference (if enrolled)
    /// </summary>
    public string? BusinessReference { get; init; }

    /// <summary>
    /// App reference (if enrolled)
    /// </summary>
    public string? AppReference { get; init; }

    /// <summary>
    /// Indicates if webhook is set up (if enrolled)
    /// </summary>
    public bool? HasWebhookSetup { get; init; }
}
