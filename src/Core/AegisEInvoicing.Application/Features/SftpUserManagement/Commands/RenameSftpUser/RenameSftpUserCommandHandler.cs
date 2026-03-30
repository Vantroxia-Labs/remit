using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models.SFTP;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Commands.RenameSftpUser;

/// <summary>
/// Handler for renaming SFTP users in database
/// </summary>
public class RenameSftpUserCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    ILogger<RenameSftpUserCommandHandler> logger) : IRequestHandler<RenameSftpUserCommand, RenameSftpUserResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<RenameSftpUserCommandHandler> _logger = logger;

    public async Task<RenameSftpUserResult> Handle(RenameSftpUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SFTP user rename from {Username} to {NewUsername}",
            request.Username, request.NewUsername);

        try
        {
            // Check if new username already exists in database
            var existingUser = await _context.SFTPUsers
                .FirstOrDefaultAsync(s => s.Username == request.NewUsername && !s.IsDeleted, cancellationToken);

            if (existingUser != null)
            {
                _logger.LogWarning("New username {NewUsername} already exists in database", request.NewUsername);
                return new RenameSftpUserResult(false, $"Username '{request.NewUsername}' already exists");
            }

            // Update username in database
            var sftpUser = await _context.SFTPUsers
                .FirstOrDefaultAsync(s => s.Username == request.Username && !s.IsDeleted, cancellationToken);

            if (sftpUser == null)
            {
                _logger.LogWarning("SFTP user not found: {Username}", request.Username);
                return new RenameSftpUserResult(false, "SFTP user not found");
            }

            var currentUserId = _currentUserService.UserId ?? Guid.Empty;

            UpdateUsernameViaReflection(sftpUser, request.NewUsername);
            sftpUser.UpdateLastSyncedAt(currentUserId);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("SFTP user renamed from {Username} to {NewUsername}",
                request.Username, request.NewUsername);

            _logger.LogInformation("Successfully renamed SFTP user from {Username} to {NewUsername}",
                request.Username, request.NewUsername);
            return new RenameSftpUserResult(true, "User renamed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming SFTP user from {Username} to {NewUsername}",
                request.Username, request.NewUsername);
            return new RenameSftpUserResult(false, $"Error renaming user: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the username using reflection since it has a private setter
    /// In a production environment, consider adding a proper method to the entity
    /// </summary>
    private static void UpdateUsernameViaReflection(SFTPUser sftpUser, string newUsername)
    {
        var usernameProperty = typeof(SFTPUser).GetProperty("Username");
        if (usernameProperty != null)
        {
            // Use reflection to set the property even if it has a private setter
            usernameProperty.SetValue(sftpUser, newUsername, null);
        }
        else
        {
            throw new InvalidOperationException("Unable to update Username property on SFTPUser entity");
        }
    }
}