//using AegisEInvoicing.Portal.API.Attributes;
//using AegisEInvoicing.Portal.API.Models;
//using AegisEInvoicing.Portal.API.Models.Business.Sftp;
//using AegisEInvoicing.Portal.API.Models.SftpUser;
//using AegisEInvoicing.Application.Features.SftpUserManagement.Commands.ChangeSftpPassword;
//using AegisEInvoicing.Application.Features.SftpUserManagement.Commands.ToggleSftpInvoiceTransmission;
//using AegisEInvoicing.Application.Features.SftpUserManagement.Queries.GetAllSftpUsers;
//using AegisEInvoicing.Application.Features.SftpUserManagement.Queries.GetSftpInvoiceTransmissionState;
//using AegisEInvoicing.Domain.Constants;
//using Microsoft.AspNetCore.Mvc;
//using Swashbuckle.AspNetCore.Annotations;

//namespace AegisEInvoicing.Portal.API.Controllers;

//public partial class BusinessController
//{
//    /// <summary>
//    /// Get SFTP user information for current business from database and Cerberus (ClientAdmin Only)
//    /// </summary>
//    /// <param name="cancellationToken">Cancellation token</param>
//    /// <returns>SFTP user information from both database and Cerberus</returns>
//    [HttpGet("sftp-user-info")]
//    [RequireRole(RoleConstants.ClientAdmin)]
//    [SwaggerOperation(
//        Summary = "Get SFTP User Information (ClientAdmin Only)",
//        Description = "Retrieves SFTP user information from both database and CerberusService for the current business. Only business administrators can access their own SFTP user information."
//    )]
//    [SwaggerResponse(200, "SFTP user information retrieved successfully", typeof(ApiResponse<BusinessSftpUserInfoResponse>))]
//    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
//    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<object>))]
//    [SwaggerResponse(404, "SFTP user not found for this business", typeof(ApiResponse<object>))]
//    public async Task<IActionResult> GetSftpUserInfoAsync(CancellationToken cancellationToken = default)
//    {
//        _logger.LogInformation("ClientAdmin requested SFTP user information for their business");

//        try
//        {
//            // Get SFTP users for the current business from database
//            var query = new GetAllSftpUsersQuery();
//            var sftpUsers = await _mediator.Send(query, cancellationToken);

//            // Filter to current business only (this should be handled by the query based on current user context)
//            var currentBusinessSftpUser = sftpUsers.FirstOrDefault();

//            if (currentBusinessSftpUser == null)
//            {
//                return NotFound(Error("No SFTP user found for this business"));
//            }

//            // Map Application DTO to API DTO
//            var apiSftpUserDto = new Models.SftpUser.SftpUserDto
//            {
//                Id = currentBusinessSftpUser.Id,
//                BusinessId = currentBusinessSftpUser.BusinessId,
//                BusinessName = currentBusinessSftpUser.BusinessName,
//                Username = currentBusinessSftpUser.Username,
//                Status = currentBusinessSftpUser.Status,
//                RootDirectoryPath = currentBusinessSftpUser.RootDirectoryPath,
//                WorkingDirectory = currentBusinessSftpUser.WorkingDirectory,
//                DirectoriesCreated = currentBusinessSftpUser.DirectoriesCreated,
//                CerberusCreatedAt = currentBusinessSftpUser.CerberusCreatedAt,
//                LastSyncedAt = currentBusinessSftpUser.LastSyncedAt,
//                CreatedAt = currentBusinessSftpUser.CreatedAt,
//                UpdatedAt = currentBusinessSftpUser.UpdatedAt
//            };

//            // Get additional user information from Cerberus
//            var cerberusResult = await _cerberusService.GetUserInformationAsync(currentBusinessSftpUser.Username);

//            CerberusUserInfoResponse? cerberusUserInfo = null;
//            if (cerberusResult.GetUserInformationResponse.result && cerberusResult.GetUserInformationResponse.UserInformation != null)
//            {
//                cerberusUserInfo = new CerberusUserInfoResponse
//                {
//                    Name = cerberusResult.GetUserInformationResponse.UserInformation.name,
//                    FirstName = cerberusResult.GetUserInformationResponse.UserInformation.fname,
//                    LastName = cerberusResult.GetUserInformationResponse.UserInformation.sname,
//                    Email = cerberusResult.GetUserInformationResponse.UserInformation.email,
//                    Description = cerberusResult.GetUserInformationResponse.UserInformation.desc,
//                    LastLogin = cerberusResult.GetUserInformationResponse.UserInformation.lastLogin,
//                    CreateDate = cerberusResult.GetUserInformationResponse.UserInformation.createDate,
//                    IsDisabled = cerberusResult.GetUserInformationResponse.UserInformation.isDisabled.value,
//                    IsAnonymous = cerberusResult.GetUserInformationResponse.UserInformation.isAnonymous.value,
//                };
//            }

//            var response = new BusinessSftpUserInfoResponse
//            {
//                DatabaseInfo = apiSftpUserDto,
//                CerberusInfo = cerberusUserInfo
//            };

//            _logger.LogInformation("Successfully retrieved SFTP user information for business SFTP user: {Username}",
//                currentBusinessSftpUser.Username);

//            return Success(response, "SFTP user information retrieved successfully");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Failed to retrieve SFTP user information for business");
//            return BadRequest(Error("Failed to retrieve SFTP user information"));
//        }
//    }

//    /// <summary>
//    /// Change SFTP user password for current business (ClientAdmin Only)
//    /// </summary>
//    /// <param name="request">Request containing new password</param>
//    /// <param name="cancellationToken">Cancellation token</param>
//    /// <returns>Operation result</returns>
//    [HttpPost("sftp-change-password")]
//    [RequireRole(RoleConstants.ClientAdmin)]
//    [SwaggerOperation(
//        Summary = "Change SFTP Password (ClientAdmin Only)",
//        Description = "Changes SFTP password for the current business in both CerberusService and database. Only business administrators can change their own SFTP password."
//    )]
//    [SwaggerResponse(200, "Password changed successfully", typeof(ApiResponse<SftpOperationResponse>))]
//    [SwaggerResponse(400, "Invalid request or password change failed", typeof(ApiResponse<object>))]
//    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
//    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<object>))]
//    [SwaggerResponse(404, "SFTP user not found for this business", typeof(ApiResponse<object>))]
//    public async Task<IActionResult> ChangeSftpPasswordAsync([FromBody] BusinessChangeSftpPasswordRequest request, CancellationToken cancellationToken = default)
//    {
//        _logger.LogInformation("ClientAdmin requested to change SFTP password for their business");

//        try
//        {
//            // Get SFTP users for the current business to find the username
//            var query = new GetAllSftpUsersQuery();
//            var sftpUsers = await _mediator.Send(query, cancellationToken);

//            var currentBusinessSftpUser = sftpUsers.FirstOrDefault();
//            if (currentBusinessSftpUser == null)
//            {
//                return NotFound(Error("No SFTP user found for this business"));
//            }

//            // Use the existing ChangeSftpPasswordCommand with the business's SFTP username
//            var command = new ChangeSftpPasswordCommand(currentBusinessSftpUser.Username, request.OldPassword, request.NewPassword);
//            var result = await _mediator.Send(command, cancellationToken);

//            if (!result.IsSuccess)
//            {
//                _logger.LogWarning("Failed to change password for business SFTP user {Username}: {Message}",
//                    currentBusinessSftpUser.Username, result.Message);
//                return BadRequest(Error(result.Message));
//            }

//            _logger.LogInformation("Successfully changed password for business SFTP user: {Username}", currentBusinessSftpUser.Username);
//            return Success(new SftpOperationResponse { Success = true, Message = result.Message },
//                result.Message);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Failed to change SFTP password for business");
//            return BadRequest(Error("Failed to change SFTP password"));
//        }
//    }

//    /// <summary>
//    /// Get SFTP connection information for current business (ClientAdmin Only)
//    /// </summary>
//    /// <param name="cancellationToken">Cancellation token</param>
//    /// <returns>SFTP connection details including host, port, and directory structure</returns>
//    [HttpGet("sftp-connection-info")]
//    [RequireRole(RoleConstants.ClientAdmin)]
//    [SwaggerOperation(
//        Summary = "Get SFTP Connection Information (ClientAdmin Only)",
//        Description = "Retrieves SFTP connection information for the current business including host, port, username, and directory structure. Only business administrators can access their own connection details."
//    )]
//    [SwaggerResponse(200, "Connection information retrieved successfully", typeof(ApiResponse<BusinessSftpConnectionInfoResponse>))]
//    [SwaggerResponse(401, "Authentication failed", typeof(ApiResponse<object>))]
//    [SwaggerResponse(403, "Access denied - insufficient permissions", typeof(ApiResponse<object>))]
//    [SwaggerResponse(404, "SFTP user not found for this business", typeof(ApiResponse<object>))]
//    public async Task<IActionResult> GetSftpConnectionInfoAsync(CancellationToken cancellationToken = default)
//    {
//        _logger.LogInformation("ClientAdmin requested SFTP connection information for their business");

//        try
//        {
//            // Get SFTP users for the current business from database
//            var query = new GetAllSftpUsersQuery();
//            var sftpUsers = await _mediator.Send(query, cancellationToken);

//            var currentBusinessSftpUser = sftpUsers.FirstOrDefault();
//            if (currentBusinessSftpUser == null)
//            {
//                return NotFound(Error("No SFTP user found for this business"));
//            }

//            // Get connection details from configuration
//            var host = _configuration["CerberusService:Host"] ?? "localhost";
//            var portString = _configuration["CerberusService:Port"] ?? "22";

//            if (!int.TryParse(portString, out var port))
//            {
//                port = 22; // Default SFTP port
//            }

//            var response = new BusinessSftpConnectionInfoResponse
//            {
//                Host = host,
//                Port = port,
//                Username = currentBusinessSftpUser.Username,
//                WorkingDirectory = currentBusinessSftpUser.WorkingDirectory,
//                //RootDirectoryPath = currentBusinessSftpUser.RootDirectoryPath,
//                DirectoriesCreated = currentBusinessSftpUser.DirectoriesCreated,
//                Status = currentBusinessSftpUser.Status.ToString(),
//                ExpectedDirectories = new List<string>
//                {
//                    "PROCESSED",
//                    "NACK",
//                    "ACK"
//                },
//                Instructions = new List<string>
//                {
//                    $"Connect to SFTP server at {host}:{port}",
//                    $"Use username: {currentBusinessSftpUser.Username}",
//                    $"Upload files to the root directory: {currentBusinessSftpUser.WorkingDirectory}",
//                    "Processed files will be moved to PROCESSED folder",
//                    "Successful processing confirmations will be in ACK folder",
//                    "Failed processing notifications will be in NACK folder"
//                }
//            };

//            _logger.LogInformation("Successfully retrieved SFTP connection information for business SFTP user: {Username}",
//                currentBusinessSftpUser.Username);

//            return Success(response, "SFTP connection information retrieved successfully");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Failed to retrieve SFTP connection information for business");
//            return BadRequest(Error("Failed to retrieve SFTP connection information"));
//        }
//    }

//    /// <summary>
//    /// Enable/Disable SFTP invoice transmission for a business's SFTP user
//    /// </summary>
//    [HttpPatch("sftp/toggle-invoice-transmission")]
//    [RequireRole(RoleConstants.ClientAdmin)]
//    [SwaggerOperation(Summary = "Toggle SFTP Invoice Transmission", Description = "Enable or disable SFTP invoice transmission for the current user's business.")]
//    [SwaggerResponse(200, "Toggle updated", typeof(ApiResponse<object>))]
//    [SwaggerResponse(400, "Business context missing", typeof(ApiResponse<object>))]
//    [SwaggerResponse(404, "SFTP user not found", typeof(ApiResponse<object>))]
//    public async Task<IActionResult> ToggleSftpInvoiceTransmission([FromBody] ToggleSftpTransmissionRequest request, CancellationToken cancellationToken = default)
//    {
//        var result = await _mediator.Send(new ToggleSftpInvoiceTransmissionCommand(request.Enabled), cancellationToken);
//        if (!result.IsSuccess)
//        {
//            var status = result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? 404 : 400;
//            return Error(result.Message, status);
//        }

//        return Success(new { businessId = result.BusinessId!.Value, enabled = result.Enabled!.Value }, "SFTP invoice transmission toggle updated");
//    }

//    /// <summary>
//    /// Get the current SFTP invoice transmission state for a business
//    /// </summary>
//    [HttpGet("sftp/invoice-transmission-state")]
//    [RequireRole(RoleConstants.ClientAdmin)]
//    [SwaggerOperation(Summary = "Get SFTP Invoice Transmission State", Description = "Returns whether SFTP invoice transmission is enabled for the current user's business.")]
//    [SwaggerResponse(200, "State retrieved", typeof(ApiResponse<object>))]
//    [SwaggerResponse(400, "Business context missing", typeof(ApiResponse<object>))]
//    [SwaggerResponse(404, "SFTP user not found", typeof(ApiResponse<object>))]
//    public async Task<IActionResult> GetSftpInvoiceTransmissionState(CancellationToken cancellationToken = default)
//    {
//        var dto = await _mediator.Send(new GetSftpInvoiceTransmissionStateQuery(), cancellationToken);
//        if (dto is null)
//        {
//            return Error("SFTP user not found for this business or business context missing", 404);
//        }

//        return Success(new { businessId = dto.BusinessId, enabled = dto.Enabled }, string.Empty);
//    }
//}
