using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Commands.DeleteSftpUser;

/// <summary>
/// Handler for deleting SFTP users from database (SFTPGo manages users dynamically)
/// </summary>
public class DeleteSftpUserCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    ILogger<DeleteSftpUserCommandHandler> logger) : IRequestHandler<DeleteSftpUserCommand, DeleteSftpUserResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<DeleteSftpUserCommandHandler> _logger = logger;

    public async Task<DeleteSftpUserResult> Handle(DeleteSftpUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SFTP user deletion for username: {Username}", request.Username);

        try
        {
            var sftpUser = await _context.SFTPUsers
                .FirstOrDefaultAsync(s => s.Username == request.Username && !s.IsDeleted, cancellationToken);

            if (sftpUser == null)
            {
                _logger.LogWarning("SFTP user not found: {Username}", request.Username);
                return new DeleteSftpUserResult(false, "SFTP user not found");
            }

            // Delete directories from file system
            try
            {
                var ftpRootPath = sftpUser.RootDirectoryPath;
                if (!string.IsNullOrEmpty(ftpRootPath) && Directory.Exists(ftpRootPath))
                {
                    Directory.Delete(ftpRootPath, recursive: true);
                    _logger.LogInformation("Deleted SFTP directories for user: {Username}", request.Username);
                }
            }
            catch (Exception dirEx)
            {
                _logger.LogWarning(dirEx, "Failed to delete SFTP directories for user: {Username}, proceeding with user deletion", request.Username);
            }

            // Soft delete the user
            _context.SFTPUsers.Remove(sftpUser);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted SFTP user: {Username}", request.Username);
            return new DeleteSftpUserResult(true, "SFTP user deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting SFTP user: {Username}", request.Username);
            return new DeleteSftpUserResult(false, $"Error deleting SFTP user: {ex.Message}");
        }
    }
}