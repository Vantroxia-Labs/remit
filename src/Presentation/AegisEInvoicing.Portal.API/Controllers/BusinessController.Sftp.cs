using AegisEInvoicing.Portal.API.Attributes;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.Business.Sftp;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetSftpCredentials;
using AegisEInvoicing.Application.Features.SftpUserManagement.Commands.ChangeSftpPassword;
using AegisEInvoicing.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AegisEInvoicing.Portal.API.Controllers;

public partial class BusinessController
{
    [HttpGet("sftp-credentials")]
    [RequireRole(RoleConstants.ClientAdmin)]
    [SwaggerOperation(
        Summary = "Get SFTP credentials",
        Description = "Returns SFTP host, port, username and status for the current business.")]
    [SwaggerResponse(200, "SFTP credentials retrieved", typeof(ApiResponse<GetSftpCredentialsResult>))]
    [SwaggerResponse(404, "SFTP user not found", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GetSftpCredentials(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetSftpCredentialsQuery(), cancellationToken);

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

    [HttpPost("sftp-change-password")]
    [RequireRole(RoleConstants.ClientAdmin)]
    [SwaggerOperation(
        Summary = "Change SFTP password",
        Description = "Verifies OTP and changes the SFTP password for the current business SFTP user.")]
    [SwaggerResponse(200, "Password changed successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Invalid OTP or password change failed", typeof(ApiResponse<object>))]
    public async Task<IActionResult> ChangeSftpPassword(
        [FromBody] ChangeSftpPasswordWithOtpRequest request,
        [FromServices] ITotpService totpService,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Otp))
            return BadRequest(Error("OTP is required."));

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(Error("New password is required."));

        if (!_currentUserService.UserId.HasValue)
            return Forbid();

        if (!int.TryParse(request.Otp.Trim(), out var otpCode))
            return BadRequest(Error("Invalid OTP format."));

        var otpValid = totpService.Verify(otpCode, $"{_currentUserService.UserId.Value}");
        if (!otpValid)
            return BadRequest(Error("Invalid or expired OTP."));

        var sftpCredentialsResult = await _mediator.Send(new GetSftpCredentialsQuery(), cancellationToken);
        if (!sftpCredentialsResult.IsSuccess || sftpCredentialsResult.Credentials is null)
        {
            if (sftpCredentialsResult.StatusCodes == 404)
                return NotFound(Error(sftpCredentialsResult.Message));

            return BadRequest(Error(sftpCredentialsResult.Message));
        }

        var command = new ChangeSftpPasswordCommand(
            sftpCredentialsResult.Credentials.Username,
            string.Empty,
            request.NewPassword.Trim());

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(Error(result.Message));

        return Success(new { success = true }, result.Message);
    }
}
