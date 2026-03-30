namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for managing SFTP directories and file system operations
/// </summary>
public interface ISftpDirectoryService
{
    /// <summary>
    /// Creates the SFTP directory structure for a user
    /// </summary>
    /// <param name="username">The SFTP username</param>
    /// <param name="rootPath">The root directory path</param>
    /// <returns>True if directories were created successfully</returns>
    Task<bool> CreateUserDirectoriesAsync(string username, string rootPath);

    /// <summary>
    /// Checks if the user directories exist
    /// </summary>
    /// <param name="username">The SFTP username</param>
    /// <param name="rootPath">The root directory path</param>
    /// <returns>True if all required directories exist</returns>
    Task<bool> CheckDirectoriesExistAsync(string username, string rootPath);

    /// <summary>
    /// Deletes the user directories
    /// </summary>
    /// <param name="username">The SFTP username</param>
    /// <param name="rootPath">The root directory path</param>
    /// <returns>True if directories were deleted successfully</returns>
    Task<bool> DeleteUserDirectoriesAsync(string username, string rootPath);

    /// <summary>
    /// Gets the full path for a user's root directory
    /// </summary>
    /// <param name="username">The SFTP username</param>
    /// <param name="baseFtpRoot">The base FTP root path</param>
    /// <returns>The full root path for the user</returns>
    string GetUserRootPath(string username, string baseFtpRoot);

    /// <summary>
    /// Validates that a directory path is safe and within allowed bounds
    /// </summary>
    /// <param name="directoryPath">The directory path to validate</param>
    /// <returns>True if the path is valid</returns>
    bool ValidateDirectoryPath(string directoryPath);
}