using Asp.Versioning;
using AegisEInvoicing.Application.Features.TinValidation;
using AegisEInvoicing.FIRSAccessPoint.Attributes;
using AegisEInvoicing.ERP.API.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AegisEInvoicing.ERP.API.Controllers;

/// <summary>
/// TIN Validation endpoints for validating Tax Identification Numbers
/// and checking MBS enrollment status
/// </summary>
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[SwaggerTag("TIN Validation")]
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
    [SwaggerOperation(
        Summary = "Validate TIN and check MBS enrollment",
        Description = @"Validates a Tax Identification Number (TIN) and checks if the buyer is enrolled on the FIRS Managed Business System (MBS) portal via Interswitch integration.

**Validation Process:**
1. **Format Check**: Validates TIN format (12 digits)
2. **MBS Lookup**: Calls Interswitch API to verify enrollment status
3. **Status Classification**:
   - **ValidAndEnrolled**: TIN is valid and buyer is registered on MBS portal
   - **InvalidOrNotEnrolled**: Either invalid TIN or valid TIN but not enrolled (Interswitch doesn't distinguish between these)
   - **Error**: Technical error during validation

**Response Details:**
- If **enrolled** (`up: true`): Returns business name, business reference, app reference, and webhook status
- If **not enrolled** or **invalid** (`up: false`): Returns combined error message
- TIN is masked in logs (shows only last 4 digits)

**Use Cases:**
- Pre-validate TIN before creating invoices
- Check buyer enrollment status before transmission
- Verify party information during onboarding

**Example Request:**
```json
{
  ""tin"": ""123456789012""
}
```

**Example Success Response (Enrolled):**
```json
{
  ""data"": {
    ""status"": ""ValidAndEnrolled"",
    ""isValid"": true,
    ""isEnrolled"": true,
    ""message"": ""TIN is valid and buyer is enrolled on the MBS portal"",
    ""businessName"": ""ABC Trading Limited"",
    ""businessReference"": ""abc-trading-limited"",
    ""appReference"": ""interswitch-limited-ap"",
    ""hasWebhookSetup"": true
  },
  ""message"": ""TIN is valid and buyer is enrolled on the MBS portal"",
  ""success"": true
}
```

**Example Failure Response (Invalid/Not Enrolled):**
```json
{
  ""data"": {
    ""status"": ""InvalidOrNotEnrolled"",
    ""isValid"": false,
    ""isEnrolled"": false,
    ""message"": ""Invalid Buyer TIN or Buyer has not been enrolled on the MBS portal"",
    ""businessName"": null
  },
  ""message"": ""Invalid Buyer TIN or Buyer has not been enrolled on the MBS portal"",
  ""success"": false
}
```

**Security:**
- TIN is masked in all log entries (e.g., `***********9012`)
- Requires authentication via JWT token
- Tenant-agnostic endpoint (shared across all businesses)",
        OperationId = "ValidateTin"
    )]
    [SwaggerResponse(200, "TIN validation completed successfully", typeof(ApiResponse<TinValidationResponse>))]
    [SwaggerResponse(400, "Invalid request - TIN is empty or malformed", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Authentication failed - valid JWT token required", typeof(ApiResponse<object>))]
    [SwaggerResponse(500, "Internal server error or Interswitch API failure", typeof(ApiResponse<object>))]
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
                return BadRequest(Error("TIN cannot be empty"));
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
                return Ok(Success(response, result.Message));
            }
            else
            {
                _logger.LogWarning("TIN validation failed for TIN: {Tin}. Reason: {Reason}", 
                    MaskTin(request.Tin), result.Message);
                return BadRequest(Error(result.Message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during TIN validation");
            return StatusCode(StatusCodes.Status500InternalServerError,
                Error("An unexpected error occurred during TIN validation"));
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
