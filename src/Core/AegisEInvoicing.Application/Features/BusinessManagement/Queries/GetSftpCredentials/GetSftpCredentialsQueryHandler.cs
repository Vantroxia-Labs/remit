using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetSftpCredentials;

public class GetSftpCredentialsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IConfiguration configuration,
    ILogger<GetSftpCredentialsQueryHandler> logger) : IRequestHandler<GetSftpCredentialsQuery, GetSftpCredentialsResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<GetSftpCredentialsQueryHandler> _logger = logger;

    public async Task<GetSftpCredentialsResult> Handle(GetSftpCredentialsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.BusinessId.HasValue)
                return (GetSftpCredentialsResult)GenericResult.AuthorizationError();

            var sftpUser = await _context.SFTPUsers
                .FirstOrDefaultAsync(s => s.BusinessId == _currentUserService.BusinessId.Value && !s.IsDeleted, cancellationToken);

            if (sftpUser is null)
                return GetSftpCredentialsResult.NotFound("No SFTP user found for this business.");

            var host = _configuration["SFTPGoService:Host"] ?? string.Empty;
            var port = int.TryParse(_configuration["SFTPGoService:Port"], out var configuredPort) ? configuredPort : 22;

            return new GetSftpCredentialsResult
            {
                IsSuccess = true,
                StatusCodes = HttpStatusCodes.OK.ToInt(),
                Message = ResponseMessages.OPERATION_SUCCESSFUL,
                Credentials = new SftpCredentialsDto(
                    sftpUser.Username,
                    host,
                    port,
                    sftpUser.Status.ToString(),
                    sftpUser.WorkingDirectory,
                    sftpUser.LastSyncedAt)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch SFTP credentials for business {BusinessId}", _currentUserService.BusinessId);
            return (GetSftpCredentialsResult)GenericResult.Failure();
        }
    }
}
