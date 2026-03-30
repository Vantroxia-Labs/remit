using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace AegisEInvoicing.SFTP.API.Services;

/// <summary>
/// Utility class to implement cache-busting strategies for SFTP file detection
/// </summary>
public static class SftpCacheBuster
{
    /// <summary>
    /// Performs aggressive directory listing with multiple cache-busting techniques
    /// </summary>
    public static async Task<List<ISftpFile>> GetFreshDirectoryListingAsync(
        SftpClient client, 
        string directoryPath, 
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..8];
        logger.LogDebug("[{SessionId}] Starting fresh directory scan for {Directory}", sessionId, directoryPath);

        var allFiles = new List<ISftpFile>();
        var attempts = 0;
        var maxAttempts = 3;

        while (attempts < maxAttempts)
        {
            try
            {
                attempts++;
                logger.LogDebug("[{SessionId}] Directory scan attempt {Attempt}/{MaxAttempts}", sessionId, attempts, maxAttempts);

                // Strategy 1: Use different directory path formats to bypass caching
                var pathsToTry = new[]
                {
                    directoryPath,
                    directoryPath.TrimEnd('/') + "/",
                    directoryPath.TrimEnd('/'),
                    Path.GetFullPath(directoryPath).Replace('\\', '/'),
                };

                foreach (var pathVariant in pathsToTry.Distinct())
                {
                    try
                    {
                        logger.LogDebug("[{SessionId}] Trying path variant: '{PathVariant}'", sessionId, pathVariant);
                        
                        // Strategy 2: Force fresh enumeration by converting to list immediately
                        var entries = await Task.Run(() => 
                        {
                            var rawEntries = client.ListDirectory(pathVariant);
                            return rawEntries.Where(f => f != null && !string.IsNullOrEmpty(f.Name)).ToList();
                        }, cancellationToken);

                        if (entries.Any())
                        {
                            logger.LogDebug("[{SessionId}] Found {Count} entries using path '{PathVariant}'", 
                                sessionId, entries.Count, pathVariant);
                            
                            // Merge with existing results, avoiding duplicates
                            foreach (var entry in entries)
                            {
                                if (!allFiles.Any(f => f.Name == entry.Name && f.FullName == entry.FullName))
                                {
                                    allFiles.Add(entry);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "[{SessionId}] Path variant '{PathVariant}' failed: {Error}", 
                            sessionId, pathVariant, ex.Message);
                        continue;
                    }
                }

                // Strategy 3: If we found files, validate them by checking individual file properties
                if (allFiles.Any())
                {
                    logger.LogDebug("[{SessionId}] Validating {Count} discovered files", sessionId, allFiles.Count);
                    
                    var validatedFiles = new List<ISftpFile>();
                    foreach (var file in allFiles)
                    {
                        try
                        {
                            // Force a fresh stat on each file to ensure it actually exists
                            if (client.Exists(file.FullName))
                            {
                                // Get fresh attributes to confirm the file is real and accessible
                                var attributes = client.GetAttributes(file.FullName);
                                if (attributes.IsRegularFile)
                                {
                                    validatedFiles.Add(file);
                                    logger.LogDebug("[{SessionId}] Validated file: {FileName} ({Size} bytes)", 
                                        sessionId, file.Name, attributes.Size);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogDebug(ex, "[{SessionId}] File validation failed for {FileName}: {Error}", 
                                sessionId, file.Name, ex.Message);
                        }
                    }
                    
                    allFiles = validatedFiles;
                }

                // If we got results or this is our final attempt, break
                if (allFiles.Any() || attempts >= maxAttempts)
                {
                    break;
                }

                // Strategy 4: Small delay and retry for server-side cache refresh
                logger.LogDebug("[{SessionId}] No files found, waiting before retry...", sessionId);
                await Task.Delay(TimeSpan.FromMilliseconds(300 * attempts), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[{SessionId}] Directory scan attempt {Attempt} failed: {Error}", 
                    sessionId, attempts, ex.Message);
                
                if (attempts >= maxAttempts)
                {
                    throw;
                }
                
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempts), cancellationToken);
            }
        }

        logger.LogInformation("[{SessionId}] Fresh directory scan completed. Found {FileCount} files after {Attempts} attempts", 
            sessionId, allFiles.Count, attempts);

        return allFiles;
    }

    /// <summary>
    /// Forces a connection refresh by performing a simple operation that should clear any cached state
    /// </summary>
    public static async Task RefreshConnectionStateAsync(SftpClient client, string workingDirectory, ILogger logger)
    {
        try
        {
            logger.LogDebug("Refreshing SFTP connection state...");
            
            // Perform operations that should clear any cached directory state
            await Task.Run(() =>
            {
                // 1. Check current directory
                var currentDir = client.WorkingDirectory;
                
                // 2. Briefly change to parent and back (if possible)
                try
                {
                    if (workingDirectory != "/")
                    {
                        var parentDir = Path.GetDirectoryName(workingDirectory.TrimEnd('/'))?.Replace('\\', '/') ?? "/";
                        client.ChangeDirectory(parentDir);
                        client.ChangeDirectory(workingDirectory);
                    }
                }
                catch
                {
                    // If directory change fails, just continue
                }
                
                // 3. Force a stat operation on the working directory
                client.GetAttributes(workingDirectory);
            });
            
            logger.LogDebug("SFTP connection state refreshed successfully");
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Connection state refresh failed (non-critical): {Error}", ex.Message);
        }
    }
}