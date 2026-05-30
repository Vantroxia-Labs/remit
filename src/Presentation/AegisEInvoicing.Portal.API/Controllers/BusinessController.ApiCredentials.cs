using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.Business.ApiCredentials;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.RotateApiKey;
using AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetApiCredentials;
using AegisEInvoicing.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AegisEInvoicing.Portal.API.Controllers;

public partial class BusinessController
{
    [HttpGet("api-credentials")]
    [RequireRole(RoleConstants.ClientAdmin)]    public async Task<IActionResult> GetApiCredentials(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetApiCredentialsQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.StatusCodes == 404)
                return NotFound(Error(result.Message));
            if (result.StatusCodes == 403)
                return Forbid();

            return BadRequest(Error(result.Message));
        }

        return Success(result.Credentials, result.Message);
    }

    [HttpPost("rotate-api-key")]
    [RequireRole(RoleConstants.ClientAdmin)]    public async Task<IActionResult> RotateApiKey([FromBody] RotateApiKeyRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Otp))
            return BadRequest(Error("OTP is required."));

        var result = await _mediator.Send(new RotateApiKeyCommand(request.Otp.Trim()), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.StatusCodes == 403)
                return Forbid();
            if (result.StatusCodes == 404)
                return NotFound(Error(result.Message));

            return BadRequest(Error(result.Message));
        }

        return Success(new { newApiKey = result.NewApiKey }, result.Message);
    }
}
