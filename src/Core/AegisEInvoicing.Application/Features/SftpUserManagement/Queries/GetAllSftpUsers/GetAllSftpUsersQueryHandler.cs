using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Queries.GetAllSftpUsers;

/// <summary>
/// Handler for retrieving all SFTP users from the database
/// </summary>
public class GetAllSftpUsersQueryHandler : IRequestHandler<GetAllSftpUsersQuery, IEnumerable<SftpUserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetAllSftpUsersQueryHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    public GetAllSftpUsersQueryHandler(
        IApplicationDbContext context,
        ILogger<GetAllSftpUsersQueryHandler> logger,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<SftpUserDto>> Handle(GetAllSftpUsersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving SFTP users from database");

        try
        {
            // Get current user ID
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                _logger.LogWarning("Current user ID is not available");
                throw new UnauthorizedAccessException("User authentication required");
            }

            _logger.LogInformation("Looking up business ID for current user: {UserId}", currentUserId.Value);

            // Get the BusinessId from the Users table using the current user ID
            var currentUser = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == currentUserId.Value)
                .Select(u => new { u.BusinessId, u.IsAegisUser })
                .FirstOrDefaultAsync(cancellationToken);

            if (currentUser == null)
            {
                _logger.LogWarning("Current user not found in database: {UserId}", currentUserId.Value);
                throw new UnauthorizedAccessException("User not found");
            }

            var sftpUsersQuery = _context.SFTPUsers
                .AsNoTracking()
                .Include(s => s.Business)
                .Where(s => !s.IsDeleted);

            // Apply business filtering based on user type
            if (currentUser.IsAegisUser)
            {
                // KMPG users can see all SFTP users
                _logger.LogInformation("Current user is KMPG user, retrieving all SFTP users");
            }
            else if (currentUser.BusinessId.HasValue)
            {
                // Business users can only see SFTP users from their business
                _logger.LogInformation("Current user is business user, filtering by BusinessId: {BusinessId}", currentUser.BusinessId.Value);
                sftpUsersQuery = sftpUsersQuery.Where(s => s.BusinessId == currentUser.BusinessId.Value);
            }
            else
            {
                _logger.LogWarning("Non-KMPG user without BusinessId: {UserId}", currentUserId.Value);
                throw new UnauthorizedAccessException("User must be associated with a business");
            }

            var sftpUsers = await sftpUsersQuery
                .Select(s => new SftpUserDto
                {
                    Id = s.Id,
                    BusinessId = s.BusinessId,
                    BusinessName = s.Business != null ? s.Business.Name : "No Business",
                    Username = s.Username,
                    Status = s.Status,
                    RootDirectoryPath = s.RootDirectoryPath,
                    WorkingDirectory = s.WorkingDirectory,
                    DirectoriesCreated = s.DirectoriesCreated,
                    SFTPGoCreatedAt = s.SFTPGoCreatedAt,
                    LastSyncedAt = s.LastSyncedAt,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .OrderBy(s => s.BusinessName)
                .ThenBy(s => s.Username)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Successfully retrieved {Count} SFTP users from database", sftpUsers.Count);
            return sftpUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SFTP users from database");
            throw;
        }
    }
}