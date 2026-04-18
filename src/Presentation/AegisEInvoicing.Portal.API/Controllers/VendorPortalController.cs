using AegisEInvoicing.Application.Features.VendorManagement.Commands.RequestVendorOtp;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.SaveVendorDraft;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.SubmitVendorInvoice;
using AegisEInvoicing.Application.Features.VendorManagement.Commands.VerifyVendorOtp;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorPortalForm;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Public vendor portal endpoints — no authentication required.
/// Vendors access these via a unique per-broadcast token link.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/vendor-portal")]
public class VendorPortalController(IMediator mediator, ILogger<VendorPortalController> logger) : BaseApiController
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<VendorPortalController> _logger = logger;

    /// <summary>Load broadcast form details by token (no auth required).</summary>
    [HttpGet("form/{token}")]
    public async Task<IActionResult> GetFormAsync([FromRoute] string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(Error("Token is required."));

        var result = await _mediator.Send(new GetVendorPortalFormQuery(token), cancellationToken);
        if (result is null)
            return Error("Invalid or expired link.", 404);
        return Success(result, "Form loaded.");
    }

    /// <summary>Request a one-time password be sent to the vendor's registered email.</summary>
    [HttpPost("request-otp/{token}")]
    public async Task<IActionResult> RequestOtpAsync([FromRoute] string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(Error("Token is required."));

        var result = await _mediator.Send(new RequestVendorOtpCommand(token), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(Error(result.Message));
        return Success(new { result.Message }, result.Message);
    }

    /// <summary>Verify the OTP submitted by the vendor.</summary>
    [HttpPost("verify-otp/{token}")]
    public async Task<IActionResult> VerifyOtpAsync([FromRoute] string token, [FromBody] VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(Error("Token is required."));

        var result = await _mediator.Send(new VerifyVendorOtpCommand(token, request.Otp), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(Error(result.Message));
        return Success(new
        {
            result.VendorBusinessName,
            result.VendorEmail,
            result.VendorPhone,
            result.Message
        }, result.Message);
    }

    /// <summary>Save a draft of the vendor's invoice (can be called multiple times).</summary>
    [HttpPost("save-draft/{token}")]
    public async Task<IActionResult> SaveDraftAsync([FromRoute] string token, [FromBody] VendorInvoicePayload payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(Error("Token is required."));

        var result = await _mediator.Send(new SaveVendorDraftCommand(token, payload.LineItems), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(Error(result.Message));
        return Success(new { result.Message }, result.Message);
    }

    /// <summary>Submit the vendor's invoice for processing.</summary>
    [HttpPost("submit/{token}")]
    public async Task<IActionResult> SubmitAsync([FromRoute] string token, [FromBody] VendorInvoicePayload payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(Error("Token is required."));

        var result = await _mediator.Send(new SubmitVendorInvoiceCommand(token, payload.LineItems), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(Error(result.Message));
        return Success(new { result.Message }, result.Message);
    }
}

// ── Request Models ────────────────────────────────────────────────────────────

public record VerifyOtpRequest(string Otp);

public record VendorInvoicePayload(List<VendorPortalLineItemDto> LineItems);
