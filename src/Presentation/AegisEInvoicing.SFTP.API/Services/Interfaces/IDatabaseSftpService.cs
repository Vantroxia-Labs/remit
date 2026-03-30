using AegisEInvoicing.SFTP.API.Configuration;

namespace AegisEInvoicing.SFTP.API.Services.Interfaces;

/// <summary>
/// Interface for database SFTP operations
/// </summary>
public interface IDatabaseSftpService
{
    /// <summary>
    /// Gets all enabled SFTP connections from the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of SFTP connection details</returns>
    Task<List<SftpConnectionDetails>> GetEnabledSftpConnectionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables SFTP invoice transmission for a specific user after processing
    /// </summary>
    /// <param name="username">SFTP username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DisableSftpInvoiceTransmissionAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific SFTP connection by connection ID
    /// </summary>
    /// <param name="connectionId">Connection ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SFTP connection details or null if not found</returns>
    Task<SftpConnectionDetails?> GetSftpConnectionAsync(string connectionId, CancellationToken cancellationToken = default);
}