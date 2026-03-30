using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace AegisEInvoicing.Infrastructure.Services;

/// <summary>
/// Service for managing SFTP directories and file system operations
/// </summary>
public class SftpDirectoryService : ISftpDirectoryService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SftpDirectoryService> _logger;

    public SftpDirectoryService(
        IConfiguration configuration,
        ILogger<SftpDirectoryService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> CreateUserDirectoriesAsync(string username, string rootPath)
    {
        try
        {
            _logger.LogInformation("Creating SFTP directories for user: {Username} at path: {RootPath}", username, rootPath);

            if (!ValidateDirectoryPath(rootPath))
            {
                _logger.LogError("Invalid root directory path: {RootPath}", rootPath);
                return false;
            }

            var userRootDirectory = Path.Combine(rootPath, username);
            var requiredDirectories = new[]
            {
                userRootDirectory,
                Path.Combine(userRootDirectory, "PROCESSED"),
                Path.Combine(userRootDirectory, "NACK"),
                Path.Combine(userRootDirectory, "ACK")
            };

            foreach (var directory in requiredDirectories)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogInformation("Created directory: {Directory}", directory);
                }
                else
                {
                    _logger.LogInformation("Directory already exists: {Directory}", directory);
                }
                
                // Set directory permissions (read, write, list)
                await SetDirectoryPermissionsAsync(directory);
            }

            _logger.LogInformation("Successfully created SFTP directory structure for user: {Username}", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SFTP directories for user: {Username} at path: {RootPath}", username, rootPath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CheckDirectoriesExistAsync(string username, string rootPath)
    {
        try
        {
            if (!ValidateDirectoryPath(rootPath))
            {
                return false;
            }

            var userRootDirectory = Path.Combine(rootPath, username);
            var requiredDirectories = new[]
            {
                userRootDirectory,
                Path.Combine(userRootDirectory, "PROCESSED"),
                Path.Combine(userRootDirectory, "NACK"),
                Path.Combine(userRootDirectory, "ACK")
            };

            var allExist = requiredDirectories.All(Directory.Exists);

            _logger.LogInformation("Directory existence check for user {Username}: {AllExist}", username, allExist);
            
            return await Task.FromResult(allExist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check directory existence for user: {Username}", username);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserDirectoriesAsync(string username, string rootPath)
    {
        try
        {
            _logger.LogInformation("Deleting SFTP directories for user: {Username} at path: {RootPath}", username, rootPath);

            if (!ValidateDirectoryPath(rootPath))
            {
                _logger.LogError("Invalid root directory path: {RootPath}", rootPath);
                return false;
            }

            var userRootDirectory = Path.Combine(rootPath, username);

            if (Directory.Exists(userRootDirectory))
            {
                Directory.Delete(userRootDirectory, recursive: true);
                _logger.LogInformation("Successfully deleted directory: {Directory}", userRootDirectory);
            }
            else
            {
                _logger.LogInformation("Directory does not exist: {Directory}", userRootDirectory);
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete SFTP directories for user: {Username} at path: {RootPath}", username, rootPath);
            return false;
        }
    }

    /// <inheritdoc />
    public string GetUserRootPath(string username, string baseFtpRoot)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or empty", nameof(username));

        if (string.IsNullOrWhiteSpace(baseFtpRoot))
            throw new ArgumentException("Base FTP root cannot be null or empty", nameof(baseFtpRoot));

        return Path.Combine(baseFtpRoot, username);
    }

    /// <inheritdoc />
    public bool ValidateDirectoryPath(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return false;

        try
        {
            // Check for path traversal attempts
            var normalizedPath = Path.GetFullPath(directoryPath);
            
            // Get the base FTP root from configuration
            var baseFtpRoot = _configuration["SftpConfiguration:FtpRootPath"] ?? "C:\\ftproot";
            var normalizedBasePath = Path.GetFullPath(baseFtpRoot);

            // Ensure the path is within the allowed base directory
            if (!normalizedPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Directory path is outside allowed base path. Path: {Path}, Base: {Base}", 
                    normalizedPath, normalizedBasePath);
                return false;
            }

            // Check for invalid characters
            var invalidChars = Path.GetInvalidPathChars();
            if (directoryPath.Any(c => invalidChars.Contains(c)))
            {
                _logger.LogWarning("Directory path contains invalid characters: {Path}", directoryPath);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating directory path: {Path}", directoryPath);
            return false;
        }
    }

    private async Task SetDirectoryPermissionsAsync(string directoryPath)
    {
        try
        {
            _logger.LogInformation("Setting directory permissions (read, write, list) for: {Directory}", directoryPath);

            if (OperatingSystem.IsWindows())
            {
                await SetWindowsDirectoryPermissionsAsync(directoryPath);
            }
            else
            {
                await SetUnixDirectoryPermissionsAsync(directoryPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set directory permissions for: {Directory}", directoryPath);
        }
    }

    [SupportedOSPlatform("windows")]
    private async Task SetWindowsDirectoryPermissionsAsync(string directoryPath)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            var directorySecurity = directoryInfo.GetAccessControl();

            // Get current user identity for setting permissions
            var currentUser = WindowsIdentity.GetCurrent();
            var currentUserAccount = currentUser.Name;

            // Define comprehensive permissions: Read, Write, List, Delete, Upload, Download
            var permissions = FileSystemRights.Read |                          // Download files
                            FileSystemRights.Write |                         // Upload/modify files
                            FileSystemRights.ExecuteFile |                   // Execute files
                            FileSystemRights.ListDirectory |                 // List directory contents
                            FileSystemRights.CreateFiles |                   // Upload new files
                            FileSystemRights.CreateDirectories |             // Create subdirectories
                            FileSystemRights.Delete |                        // Delete files
                            FileSystemRights.DeleteSubdirectoriesAndFiles |  // Delete directories and contents
                            FileSystemRights.Modify |                        // General modify permissions
                            FileSystemRights.ReadAndExecute |                // Read and execute permissions
                            FileSystemRights.Traverse;                       // Directory traversal

            // Create access rule for current user
            var accessRule = new FileSystemAccessRule(
                currentUserAccount,
                permissions,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow);

            // Set the access rule
            directorySecurity.SetAccessRule(accessRule);

            // Also set permissions for IIS_IUSRS and IUSR if they exist (common for web applications)
            var commonWebUsers = new[] { "IIS_IUSRS", "IUSR" };
            foreach (var webUser in commonWebUsers)
            {
                try
                {
                    var webUserAccessRule = new FileSystemAccessRule(
                        webUser,
                        permissions,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow);
                    
                    directorySecurity.SetAccessRule(webUserAccessRule);
                    _logger.LogDebug("Set permissions for web user: {WebUser}", webUser);
                }
                catch (IdentityNotMappedException)
                {
                    // User doesn't exist on this system, skip
                    _logger.LogDebug("Web user {WebUser} not found on system, skipping", webUser);
                }
            }

            // Apply the security settings
            directoryInfo.SetAccessControl(directorySecurity);

            _logger.LogInformation("Successfully set Windows directory permissions for: {Directory}", directoryPath);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Windows directory permissions for: {Directory}", directoryPath);
            throw;
        }
    }

    private async Task SetUnixDirectoryPermissionsAsync(string directoryPath)
    {
        try
        {
            // For Unix systems, set directory permissions to 755 (rwxr-xr-x)
            // This gives read, write, execute to owner and read, execute to group and others
            if (File.Exists("/bin/chmod") || File.Exists("/usr/bin/chmod"))
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"755 \"{directoryPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("Successfully set Unix directory permissions (755) for: {Directory}", directoryPath);
                }
                else
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    _logger.LogWarning("chmod command failed for {Directory}: {Error}", directoryPath, error);
                }
            }
            else
            {
                _logger.LogWarning("chmod command not found, cannot set Unix permissions for: {Directory}", directoryPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Unix directory permissions for: {Directory}", directoryPath);
            throw;
        }
    }
}