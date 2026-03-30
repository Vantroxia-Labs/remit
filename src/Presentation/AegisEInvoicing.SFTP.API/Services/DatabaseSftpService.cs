using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.SFTP.API.Configuration;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AegisEInvoicing.SFTP.API.Services;

/// <summary>
/// Database service to handle SFTP user operations for the file processing service
/// </summary>
public class DatabaseSftpService(
    IApplicationDbContext dbContext,
    IOptions<SftpConfiguration> sftpConfig,
    ILogger<DatabaseSftpService> logger) : IDatabaseSftpService
{
    private readonly IApplicationDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly SftpConfiguration _sftpConfig = sftpConfig.Value ?? throw new ArgumentNullException(nameof(sftpConfig));
    private readonly ILogger<DatabaseSftpService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Gets all enabled SFTP connections from the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of SFTP connection details</returns>
    public async Task<List<SftpConnectionDetails>> GetEnabledSftpConnectionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching enabled SFTP connections from database");

            var enabledSftpUsers = await _dbContext.SFTPUsers
                .Where(u => u.SftpInvoiceTransmissionEnabled && u.Status == Domain.Enums.SFTPUserStatus.Active)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} enabled SFTP users for invoice transmission", enabledSftpUsers.Count);

            var connectionDetails = enabledSftpUsers.Select(user => MapToSftpConnectionDetails(user)).ToList();

            return connectionDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching enabled SFTP connections from database");
            throw;
        }
    }

    /// <summary>
    /// Disables SFTP invoice transmission for a specific user after processing
    /// </summary>
    /// <param name="username">SFTP username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DisableSftpInvoiceTransmissionAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Disabling SFTP invoice transmission for user: {Username}", username);

            var sftpUser = await _dbContext.SFTPUsers
                .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

            if (sftpUser == null)
            {
                _logger.LogWarning("SFTP user not found: {Username}", username);
                return;
            }

            if (!sftpUser.SftpInvoiceTransmissionEnabled)
            {
                _logger.LogDebug("SFTP invoice transmission already disabled for user: {Username}", username);
                return;
            }

            sftpUser.DisableInvoiceTransmission(Guid.Parse("9c17ea5c-483c-44f8-97e8-c364e6739949")); // System user ID
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully disabled SFTP invoice transmission for user: {Username}", username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling SFTP invoice transmission for user: {Username}", username);
            throw;
        }
    }

    /// <summary>
    /// Gets a specific SFTP connection by connection ID
    /// </summary>
    /// <param name="connectionId">Connection ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SFTP connection details or null if not found</returns>
    public async Task<SftpConnectionDetails?> GetSftpConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching SFTP connection for ID: {ConnectionId}", connectionId);

            var sftpUser = await _dbContext.SFTPUsers
                .FirstOrDefaultAsync(u => u.Username == connectionId && u.Status == Domain.Enums.SFTPUserStatus.Active, cancellationToken);

            if (sftpUser == null)
            {
                _logger.LogWarning("SFTP connection not found for ID: {ConnectionId}", connectionId);
                return null;
            }

            return MapToSftpConnectionDetails(sftpUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching SFTP connection for ID: {ConnectionId}", connectionId);
            throw;
        }
    }

    /// <summary>
    /// Maps an SFTPUser entity to SftpConnectionDetails for connecting to local SFTPGo server
    /// </summary>
    /// <param name="sftpUser">SFTP user entity</param>
    /// <returns>SFTP connection details</returns>
    private SftpConnectionDetails MapToSftpConnectionDetails(SFTPUser sftpUser)
    {
        // Connect to local SFTPGo server
        return new SftpConnectionDetails
        {
            ConnectionId = sftpUser.Username,
            Host = "localhost",
            UserName = sftpUser.Username,
            Password = sftpUser.Password,
            Port = 2222,
            WorkingDirectory = $"/uploads/{sftpUser.BusinessId}/Pending",
            RejectedDirectory = _sftpConfig.RejectedDirectory,
            ReceiptsDirectory = _sftpConfig.ReceiptsDirectory,
            PendingDirectory = _sftpConfig.PendingDirectory,
            InProgressDirectory = _sftpConfig.InProgressDirectory,
            FilePattern = _sftpConfig.FilePattern,
            IsEnabled = sftpUser.SftpInvoiceTransmissionEnabled,
            BusinessId = sftpUser.BusinessId,
            Description = $"SFTPGo connection for user: {sftpUser.Username}"
        };
    }
}