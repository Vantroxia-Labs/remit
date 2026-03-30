using AegisEInvoicing.SFTP.API.Configuration;
using AegisEInvoicing.SFTP.API.Models;

namespace AegisEInvoicing.SFTP.API.Services.Interfaces;

/// <summary>
/// Interface for SFTP operations
/// </summary>
public interface ISftpService
{
    /// <summary>
    /// Lists all invoice files (XML and JSON) from the Pending directory,
    /// atomically moves them to In-Progress, and returns the new In-Progress paths
    /// </summary>
    /// <param name="connectionId">The SFTP connection identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of invoice files (now in In-Progress)</returns>
    Task<List<SftpFileInfo>> ListInvoiceFilesAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all invoice files (XML and JSON) currently in the In-Progress directory
    /// without moving them. Used for diagnostics and monitoring.
    /// </summary>
    /// <param name="connectionId">The SFTP connection identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of invoice files found in In-Progress</returns>
    Task<List<SftpFileInfo>> ListInProgressFilesAsync(string connectionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Downloads a file from the SFTP server
    /// </summary>
    /// <param name="connectionId">The SFTP connection identifier</param>
    /// <param name="remoteFilePath">Remote file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File content as string</returns>
    Task<string> DownloadFileContentAsync(string connectionId, string remoteFilePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Downloads a file from the SFTP server as a stream
    /// </summary>
    /// <param name="connectionId">The SFTP connection identifier</param>
    /// <param name="remoteFilePath">Remote file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File content as memory stream</returns>
    Task<MemoryStream> DownloadFileStreamAsync(string connectionId, string remoteFilePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Uploads a file to the SFTP server
    /// </summary>
    /// <param name="connectionId">The SFTP connection identifier</param>
    /// <param name="remoteFilePath">Remote file path where the file will be uploaded</param>
    /// <param name="content">File content to upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UploadFileAsync(string connectionId, string remoteFilePath, string content, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Uploads a file to the SFTP server from stream
    /// </summary>
    /// <param name="connectionId">The SFTP connection identifier</param>
    /// <param name="remoteFilePath">Remote file path where the file will be uploaded</param>
    /// <param name="contentStream">File content stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UploadFileAsync(string connectionId, string remoteFilePath, Stream contentStream, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Moves a file to a different directory on the SFTP server
    /// </summary>
    /// <param name="connectionId">The SFTP connection identifier</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="destinationFilePath">Destination file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MoveFileAsync(string connectionId, string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a file from the SFTP server
    /// </summary>
    /// <param name="connectionId">The SFTP connection identifier</param>
    /// <param name="remoteFilePath">Remote file path to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteFileAsync(string connectionId, string remoteFilePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a directory on the SFTP server if it doesn't exist
    /// </summary>
    /// <param name="connectionId">The SFTP connection identifier</param>
    /// <param name="remoteDirectoryPath">Remote directory path to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CreateDirectoryIfNotExistsAsync(string connectionId, string remoteDirectoryPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests connectivity to a specific SFTP connection
    /// </summary>
    /// <param name="connectionId">The SFTP connection identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is successful, false otherwise</returns>
    Task<bool> TestConnectionAsync(string connectionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests connectivity to all configured SFTP connections
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of connection results</returns>
    Task<Dictionary<string, bool>> TestAllConnectionsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets information about a specific SFTP connection
    /// </summary>
    /// <param name="connectionId">The SFTP connection identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connection details</returns>
    Task<SftpConnectionDetails?> GetConnectionDetailsAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configured SFTP connections
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all connection details</returns>
    Task<List<SftpConnectionDetails>> GetAllConnectionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets enabled SFTP connections
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of enabled connection details</returns>
    Task<List<SftpConnectionDetails>> GetEnabledConnectionsAsync(CancellationToken cancellationToken = default);
}