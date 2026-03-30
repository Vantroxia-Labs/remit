using AegisEInvoicing.SFTP.API.Models;

namespace AegisEInvoicing.SFTP.API.Services.Interfaces;

/// <summary>
/// Interface for orchestrating the complete file processing workflow
/// </summary>
public interface IFileProcessingService
{
    /// <summary>
    /// Processes all pending XML files from all enabled SFTP connections
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing statistics</returns>
    Task<ProcessingStatistics> ProcessAllPendingFilesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes files from a specific SFTP connection
    /// </summary>
    /// <param name="connectionId">SFTP connection identifier</param>
    /// <param name="maxFiles">Maximum number of files to process (null for unlimited)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of processing results</returns>
    Task<List<FileProcessingResult>> ProcessFilesFromConnectionAsync(
        string connectionId, 
        int? maxFiles = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes a single file
    /// </summary>
    /// <param name="fileInfo">File information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    Task<FileProcessingResult> ProcessSingleFileAsync(
        SftpFileInfo fileInfo, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current service status
    /// </summary>
    /// <returns>Service status information</returns>
    Task<ServiceStatus> GetServiceStatusAsync();
    
    /// <summary>
    /// Performs health checks on all dependencies
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check results</returns>
    Task<Dictionary<string, bool>> PerformHealthChecksAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cleans up processed files and old response files
    /// </summary>
    /// <param name="olderThanDays">Remove files older than specified days</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of files cleaned up</returns>
    Task<int> CleanupOldFilesAsync(int olderThanDays = 30, CancellationToken cancellationToken = default);
}