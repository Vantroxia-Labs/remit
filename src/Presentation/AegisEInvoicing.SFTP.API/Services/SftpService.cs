using AegisEInvoicing.SFTP.API.Configuration;
using AegisEInvoicing.SFTP.API.Models;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System.Collections.Concurrent;
using System.Text;
using SshConnectionInfo = Renci.SshNet.ConnectionInfo;

namespace AegisEInvoicing.SFTP.API.Services;

/// <summary>
/// Implementation of SFTP service with connection pooling and resilience
/// </summary>
public class SftpService : ISftpService, IDisposable
{
    private readonly SftpConfiguration _sftpConfig;
    private readonly IDatabaseSftpService _databaseSftpService;
    private readonly ILogger<SftpService> _logger;
    private readonly ConcurrentDictionary<string, SftpConnectionPool> _connectionPools;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;

    public SftpService(
        IOptions<SftpConfiguration> sftpConfig,
        IDatabaseSftpService databaseSftpService,
        ILogger<SftpService> logger)
    {
        _sftpConfig = sftpConfig.Value ?? throw new ArgumentNullException(nameof(sftpConfig));
        _databaseSftpService = databaseSftpService ?? throw new ArgumentNullException(nameof(databaseSftpService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionPools = new ConcurrentDictionary<string, SftpConnectionPool>();
        _semaphore = new SemaphoreSlim(_sftpConfig.MaxConcurrentConnections);
        
        // Initialize connection pools (will be done dynamically now)
        // InitializeConnectionPools();
        
        // Configure retry policy
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: _sftpConfig.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(_sftpConfig.RetryDelayMilliseconds * Math.Pow(2, retryAttempt - 1)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("SFTP operation failed, retrying in {Delay}ms. Attempt {RetryCount}/{MaxRetries}. Error: {Error}",
                        timespan.TotalMilliseconds, retryCount, _sftpConfig.MaxRetryAttempts, outcome.InnerException?.Message);
                });
    }

    public async Task<List<SftpFileInfo>> ListInvoiceFilesAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing invoice files (XML/JSON) for connection: {ConnectionId}", connectionId);

        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (connectionDetails == null)
        {
            throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            await _semaphore.WaitAsync(cancellationToken);
            SftpClient? client = null;
            try
            {
                client = await GetSftpClientAsync(connectionId);
                var files = new List<SftpFileInfo>();

                // Build Pending and In-Progress directory paths
                var baseDirectory = Path.GetDirectoryName(connectionDetails.WorkingDirectory)?.Replace('\\', '/') ?? "/";
                var pendingDir = Path.Combine(baseDirectory, connectionDetails.PendingDirectory).Replace('\\', '/');
                var inProgressDir = Path.Combine(baseDirectory, connectionDetails.InProgressDirectory).Replace('\\', '/');

                _logger.LogDebug("Scanning Pending directory: {Directory} on connection {ConnectionId}", 
                    pendingDir, connectionId);

                // Ensure directories exist
                await CreateDirectoryIfNotExistsWithClientAsync(client, pendingDir);
                await CreateDirectoryIfNotExistsWithClientAsync(client, inProgressDir);

                // Get fresh directory listing using cache-busting techniques
                await SftpCacheBuster.RefreshConnectionStateAsync(client, pendingDir, _logger);
                var sftpFiles = await SftpCacheBuster.GetFreshDirectoryListingAsync(
                    client, 
                    pendingDir, 
                    _logger, 
                    cancellationToken);

                // Filter for XML and JSON files
                var invoiceFiles = sftpFiles.Where(f => f.IsRegularFile && 
                    (f.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                     f.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))).ToList();

                _logger.LogInformation("Found {InvoiceFileCount} invoice files (XML/JSON) out of {TotalFiles} in Pending for connection {ConnectionId}",
                    invoiceFiles.Count, sftpFiles.Count, connectionId);

                // Atomically move Pending → In-Progress before returning
                foreach (var file in invoiceFiles)
                {
                    try
                    {
                        var sourceFilePath = file.FullName;
                        var destFilePath = Path.Combine(inProgressDir, file.Name).Replace('\\', '/');

                        // Check if already exists in In-Progress
                        if (client.Exists(destFilePath))
                        {
                            var baseName = Path.GetFileNameWithoutExtension(file.Name);
                            var ext = Path.GetExtension(file.Name);
                            var version = 2;
                            while (client.Exists(destFilePath))
                            {
                                destFilePath = Path.Combine(inProgressDir, $"{baseName}_V{version}{ext}").Replace('\\', '/');
                                version++;
                            }
                        }

                        client.RenameFile(sourceFilePath, destFilePath);
                        _logger.LogDebug("Moved {FileName} from Pending to In-Progress", file.Name);

                        files.Add(new SftpFileInfo
                        {
                            FileName = Path.GetFileName(destFilePath),
                            FullPath = destFilePath,
                            Size = file.Length,
                            LastModified = file.LastWriteTime,
                            ConnectionId = connectionId
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to move {FileName} from Pending to In-Progress", file.Name);
                    }
                }

                return files;
            }
            finally
            {
                if (client != null)
                {
                    ReturnClientToPool(connectionId, client);
                }
                _semaphore.Release();
            }
        });
    }

    public async Task<List<SftpFileInfo>> ListInProgressFilesAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing In-Progress invoice files for connection: {ConnectionId}", connectionId);

        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (connectionDetails == null)
        {
            throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            await _semaphore.WaitAsync(cancellationToken);
            SftpClient? client = null;
            try
            {
                client = await GetSftpClientAsync(connectionId);
                var files = new List<SftpFileInfo>();

                // Build In-Progress directory path
                var baseDirectory = Path.GetDirectoryName(connectionDetails.WorkingDirectory)?.Replace('\\', '/') ?? "/";
                var inProgressDir = Path.Combine(baseDirectory, connectionDetails.InProgressDirectory).Replace('\\', '/');

                _logger.LogDebug("Refreshing directory listing for In-Progress: {Directory} on connection {ConnectionId}", 
                    inProgressDir, connectionId);

                // First, refresh connection state to clear any cached directory information
                await SftpCacheBuster.RefreshConnectionStateAsync(client, inProgressDir, _logger);

                // Get fresh directory listing using cache-busting techniques
                var sftpFiles = await SftpCacheBuster.GetFreshDirectoryListingAsync(
                    client, 
                    inProgressDir, 
                    _logger, 
                    cancellationToken);

                // Filter for XML and JSON files from the cache-busted results
                var invoiceFiles = sftpFiles.Where(f => f.IsRegularFile && 
                    (f.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                     f.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))).ToList();

                _logger.LogInformation("Cache-busting scan complete: Found {InvoiceFileCount} invoice files (XML/JSON) out of {TotalFiles} total files in {Directory}",
                    invoiceFiles.Count, sftpFiles.Count, inProgressDir);

                foreach (var file in invoiceFiles)
                {
                    _logger.LogDebug("Found XML file: {FileName} (Size: {Size} bytes, Modified: {LastModified})", 
                        file.Name, file.Length, file.LastWriteTime);
                        
                    files.Add(new SftpFileInfo
                    {
                        FileName = file.Name,
                        FullPath = file.FullName,
                        Size = file.Length,
                        LastModified = file.LastWriteTime,
                        ConnectionId = connectionId
                    });
                }
                
                _logger.LogInformation("Listed {FileCount} XML files in {Directory} for connection {ConnectionId}",
                    files.Count, connectionDetails.WorkingDirectory, connectionId);
                
                return files;
            }
            finally
            {
                if (client != null)
                {
                    ReturnClientToPool(connectionId, client);
                }
                _semaphore.Release();
            }
        });
    }

    public async Task<string> DownloadFileContentAsync(string connectionId, string remoteFilePath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Downloading file content: {FilePath} from connection: {ConnectionId}", remoteFilePath, connectionId);

        using var stream = await DownloadFileStreamAsync(connectionId, remoteFilePath, cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync(cancellationToken);
        
        _logger.LogDebug("Downloaded file content ({Length} characters) from {FilePath}", content.Length, remoteFilePath);
        return content;
    }

    public async Task<MemoryStream> DownloadFileStreamAsync(string connectionId, string remoteFilePath, CancellationToken cancellationToken = default)
    {
        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (connectionDetails == null)
        {
            throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            await _semaphore.WaitAsync(cancellationToken);
            SftpClient? client = null;
            try
            {
                client = await GetSftpClientAsync(connectionId);
                var memoryStream = new MemoryStream();
                
                client.DownloadFile(remoteFilePath, memoryStream);
                memoryStream.Position = 0;
                
                _logger.LogDebug("Downloaded file stream ({Length} bytes) from {FilePath}", memoryStream.Length, remoteFilePath);
                return memoryStream;
            }
            finally
            {
                if (client != null)
                {
                    ReturnClientToPool(connectionId, client);
                }
                _semaphore.Release();
            }
        });
    }

    public async Task UploadFileAsync(string connectionId, string remoteFilePath, string content, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await UploadFileAsync(connectionId, remoteFilePath, stream, cancellationToken);
    }

    public async Task UploadFileAsync(string connectionId, string remoteFilePath, Stream contentStream, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Uploading file: {FilePath} to connection: {ConnectionId}", remoteFilePath, connectionId);

        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (connectionDetails == null)
        {
            throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        }

        await _retryPolicy.ExecuteAsync(async () =>
        {
            await _semaphore.WaitAsync(cancellationToken);
            SftpClient? client = null;
            try
            {
                client = await GetSftpClientAsync(connectionId);
                
                // Ensure directory exists
                var directoryPath = Path.GetDirectoryName(remoteFilePath)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    await CreateDirectoryIfNotExistsWithClientAsync(client, directoryPath);
                }
                
                contentStream.Position = 0;
                client.UploadFile(contentStream, remoteFilePath);
                
                _logger.LogDebug("Uploaded file ({Length} bytes) to {FilePath}", contentStream.Length, remoteFilePath);
            }
            finally
            {
                if (client != null)
                {
                    ReturnClientToPool(connectionId, client);
                }
                _semaphore.Release();
            }
        });
    }

    public async Task MoveFileAsync(string connectionId, string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Moving file from {Source} to {Destination} on connection: {ConnectionId}",
            sourceFilePath, destinationFilePath, connectionId);

        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (connectionDetails == null)
        {
            throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        }

        await _retryPolicy.ExecuteAsync(async () =>
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                using var client = await GetSftpClientAsync(connectionId);
                
                // Check if source file exists
                if (!client.Exists(sourceFilePath))
                {
                    throw new FileNotFoundException($"Source file does not exist: {sourceFilePath}");
                }
                
                _logger.LogDebug("Source file exists: {Source}", sourceFilePath);
                
                // Ensure destination directory exists using the same client
                var destinationDir = Path.GetDirectoryName(destinationFilePath)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(destinationDir))
                {
                    _logger.LogDebug("Creating destination directory: {DestinationDir}", destinationDir);
                    await CreateDirectoryIfNotExistsWithClientAsync(client, destinationDir);
                    _logger.LogDebug("Destination directory confirmed: {DestinationDir}", destinationDir);
                }
                
                // Check if destination file already exists
                if (client.Exists(destinationFilePath))
                {
                    _logger.LogWarning("Destination file already exists, deleting: {Destination}", destinationFilePath);
                    client.DeleteFile(destinationFilePath);
                    
                    // Small delay to ensure deletion is processed
                    await Task.Delay(100, cancellationToken);
                }
                
                _logger.LogDebug("Executing SFTP rename operation from {Source} to {Destination}", sourceFilePath, destinationFilePath);
                client.RenameFile(sourceFilePath, destinationFilePath);
                
                // Add small delay to allow SFTP server to process the move
                await Task.Delay(200, cancellationToken);
                
                // Verify the move was successful with retries
                var verificationAttempts = 0;
                var maxVerificationAttempts = 3;
                var moveSuccessful = false;
                
                while (verificationAttempts < maxVerificationAttempts && !moveSuccessful)
                {
                    var destinationExists = client.Exists(destinationFilePath);
                    var sourceStillExists = client.Exists(sourceFilePath);
                    
                    moveSuccessful = destinationExists && !sourceStillExists;
                    
                    if (!moveSuccessful)
                    {
                        verificationAttempts++;
                        _logger.LogDebug("Move verification attempt {Attempt}/{MaxAttempts}: Source exists: {SourceExists}, Destination exists: {DestExists}", 
                            verificationAttempts, maxVerificationAttempts, sourceStillExists, destinationExists);
                        
                        if (verificationAttempts < maxVerificationAttempts)
                        {
                            await Task.Delay(500, cancellationToken); // Wait before retry
                        }
                    }
                }
                
                if (moveSuccessful)
                {
                    _logger.LogInformation("Successfully moved file from {Source} to {Destination}", sourceFilePath, destinationFilePath);
                }
                else
                {
                    var finalSourceExists = client.Exists(sourceFilePath);
                    var finalDestExists = client.Exists(destinationFilePath);
                    throw new InvalidOperationException($"File move verification failed after {maxVerificationAttempts} attempts. Source exists: {finalSourceExists}, Destination exists: {finalDestExists}");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        });
    }

    public async Task DeleteFileAsync(string connectionId, string remoteFilePath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting file: {FilePath} from connection: {ConnectionId}", remoteFilePath, connectionId);

        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (connectionDetails == null)
        {
            throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        }

        await _retryPolicy.ExecuteAsync(async () =>
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                using var client = await GetSftpClientAsync(connectionId);
                client.DeleteFile(remoteFilePath);
                _logger.LogDebug("Deleted file: {FilePath}", remoteFilePath);
            }
            finally
            {
                _semaphore.Release();
            }
        });
    }

    public async Task CreateDirectoryIfNotExistsAsync(string connectionId, string remoteDirectoryPath, CancellationToken cancellationToken = default)
    {
        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (connectionDetails == null)
        {
            throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        }

        await _retryPolicy.ExecuteAsync(async () =>
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                using var client = await GetSftpClientAsync(connectionId);
                
                if (!client.Exists(remoteDirectoryPath))
                {
                    // Create parent directories if they don't exist
                    var pathParts = remoteDirectoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var currentPath = "/";
                    
                    foreach (var part in pathParts)
                    {
                        currentPath = Path.Combine(currentPath, part).Replace('\\', '/');
                        if (!client.Exists(currentPath))
                        {
                            client.CreateDirectory(currentPath);
                            _logger.LogDebug("Created directory: {Directory}", currentPath);
                        }
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        });
    }

    public async Task<bool> TestConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Testing connection: {ConnectionId}", connectionId);

        try
        {
            var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
            if (connectionDetails == null)
            {
                _logger.LogWarning("Connection details not found for: {ConnectionId}", connectionId);
                return false;
            }

            using var client = await GetSftpClientAsync(connectionId);
            var testResult = client.Exists(connectionDetails.WorkingDirectory);
            
            _logger.LogDebug("Connection test {Result} for: {ConnectionId}", 
                testResult ? "successful" : "failed", connectionId);
            
            return testResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for: {ConnectionId}", connectionId);
            return false;
        }
    }

    public async Task<Dictionary<string, bool>> TestAllConnectionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing all SFTP connections");

        var results = new Dictionary<string, bool>();
        var enabledConnections = await GetEnabledConnectionsAsync(cancellationToken).ConfigureAwait(false);

        var testTasks = enabledConnections.Select(async connection =>
        {
            var result = await TestConnectionAsync(connection.ConnectionId, cancellationToken);
            return new KeyValuePair<string, bool>(connection.ConnectionId, result);
        });

        var testResults = await Task.WhenAll(testTasks);
        
        foreach (var result in testResults)
        {
            results[result.Key] = result.Value;
        }

        var successCount = results.Count(r => r.Value);
        _logger.LogInformation("Connection tests completed: {SuccessCount}/{TotalCount} successful",
            successCount, results.Count);

        return results;
    }

    public async Task<SftpConnectionDetails?> GetConnectionDetailsAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _databaseSftpService.GetSftpConnectionAsync(connectionId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get connection details for {ConnectionId}", connectionId);
            return null;
        }
    }

    public async Task<List<SftpConnectionDetails>> GetAllConnectionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _databaseSftpService.GetEnabledSftpConnectionsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all connections from database");
            return new List<SftpConnectionDetails>();
        }
    }

    public async Task<List<SftpConnectionDetails>> GetEnabledConnectionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _databaseSftpService.GetEnabledSftpConnectionsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get enabled connections from database");
            return new List<SftpConnectionDetails>();
        }
    }

    private async Task InitializeConnectionPoolsAsync()
    {
        try
        {
            var enabledConnections = await _databaseSftpService.GetEnabledSftpConnectionsAsync().ConfigureAwait(false);
            foreach (var connection in enabledConnections)
            {
                _connectionPools[connection.ConnectionId] = new SftpConnectionPool(connection, _logger);
                _logger.LogInformation("Initialized connection pool for: {ConnectionId}", connection.ConnectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize connection pools from database");
        }
    }

    private async Task<SftpClient> GetSftpClientAsync(string connectionId)
    {
        if (!_connectionPools.TryGetValue(connectionId, out var pool))
        {
            // Try to create the connection pool dynamically from database
            var connectionDetails = await _databaseSftpService.GetSftpConnectionAsync(connectionId);
            if (connectionDetails == null)
            {
                throw new InvalidOperationException($"Connection '{connectionId}' not found in database");
            }
            
            pool = new SftpConnectionPool(connectionDetails, _logger);
            _connectionPools[connectionId] = pool;
            _logger.LogInformation("Dynamically initialized connection pool for: {ConnectionId}", connectionId);
        }

        var client = await pool.GetClientAsync();
        return client;
    }

    private void ReturnClientToPool(string connectionId, SftpClient client)
    {
        if (_connectionPools.TryGetValue(connectionId, out var pool))
        {
            pool.ReturnClient(client);
        }
        else
        {
            client?.Dispose();
        }
    }

    /// <summary>
    /// Creates directory using an existing SFTP client to avoid connection overhead
    /// </summary>
    private async Task CreateDirectoryIfNotExistsWithClientAsync(SftpClient client, string remoteDirectoryPath)
    {
        await Task.Run(() => 
        {
            if (!client.Exists(remoteDirectoryPath))
            {
                // Create parent directories if they don't exist
                var pathParts = remoteDirectoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var currentPath = "/";
                
                foreach (var part in pathParts)
                {
                    currentPath = Path.Combine(currentPath, part).Replace('\\', '/');
                    if (!client.Exists(currentPath))
                    {
                        client.CreateDirectory(currentPath);
                        _logger.LogDebug("Created directory: {Directory}", currentPath);
                    }
                }
            }
        });
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var pool in _connectionPools.Values)
        {
            pool?.Dispose();
        }
        _connectionPools.Clear();

        _semaphore?.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Connection pool for SFTP clients
/// </summary>
internal class SftpConnectionPool : IDisposable
{
    private readonly SftpConnectionDetails _connectionDetails;
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<SftpClient> _availableClients;
    private readonly SemaphoreSlim _semaphore;
    private readonly object _lock = new();
    private bool _disposed;

    public SftpConnectionPool(SftpConnectionDetails connectionDetails, ILogger logger)
    {
        _connectionDetails = connectionDetails;
        _logger = logger;
        _availableClients = new ConcurrentQueue<SftpClient>();
        _semaphore = new SemaphoreSlim(10); // Max 10 connections per pool
    }

    public async Task<SftpClient> GetClientAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            // Always create fresh connections to avoid stale directory listings
            // This fixes the issue where new files aren't detected until container restart
            var client = CreateNewClient();
            
            // Ensure clean connection state
            if (client.IsConnected)
            {
                client.Disconnect();
            }
            
            // Connect with timeout and additional logging
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var connectionId = Guid.NewGuid().ToString("N")[..8];
            
            _logger.LogDebug("[{ConnectionId}] Establishing fresh SFTP connection to {Host}:{Port}", 
                connectionId, _connectionDetails.Host, _connectionDetails.Port);
            
            await Task.Run(() => client.Connect(), cts.Token);
            
            _logger.LogDebug("[{ConnectionId}] Connection established. Testing with directory listing...", connectionId);
            
            // Verify connection with a simple listing to prime the connection
            await Task.Run(() => 
            {
                var testListing = client.ListDirectory(".").Take(1).ToList();
                return testListing;
            }, cts.Token);
            
            _logger.LogDebug("[{ConnectionId}] Connection verified and ready for use", connectionId);

            return client;
        }
        catch
        {
            _semaphore.Release();
            throw;
        }
    }

    public void ReturnClient(SftpClient client)
    {
        // Always dispose connections since we create fresh ones each time
        // This ensures no stale connections are cached
        client?.Dispose();
        _semaphore.Release();
    }

    private SftpClient CreateNewClient()
    {
        var connectionInfo = CreateConnectionInfo();
        var client = new SftpClient(connectionInfo);
        
        // Set timeouts for responsive operations
        client.OperationTimeout = TimeSpan.FromSeconds(60);
        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
        
        // Add a unique identifier to help with debugging connection reuse
        var connectionId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogDebug("Created new SFTP client with ID: {ConnectionId} for {Host}", connectionId, _connectionDetails.Host);
        
        return client;
    }

    private SshConnectionInfo CreateConnectionInfo()
    {
        var authMethods = new List<AuthenticationMethod>();

        // Add password authentication if provided
        if (!string.IsNullOrEmpty(_connectionDetails.Password))
        {
            authMethods.Add(new PasswordAuthenticationMethod(_connectionDetails.UserName, _connectionDetails.Password));
        }

        // Add key-based authentication if provided
        if (!string.IsNullOrEmpty(_connectionDetails.PrivateKeyFilePath) && File.Exists(_connectionDetails.PrivateKeyFilePath))
        {
            var keyFile = string.IsNullOrEmpty(_connectionDetails.PrivateKeyPassphrase)
                ? new PrivateKeyFile(_connectionDetails.PrivateKeyFilePath)
                : new PrivateKeyFile(_connectionDetails.PrivateKeyFilePath, _connectionDetails.PrivateKeyPassphrase);

            authMethods.Add(new PrivateKeyAuthenticationMethod(_connectionDetails.UserName, keyFile));
        }

        if (authMethods.Count == 0)
        {
            throw new InvalidOperationException($"No authentication method configured for connection: {_connectionDetails.ConnectionId}");
        }

        return new SshConnectionInfo(_connectionDetails.Host, _connectionDetails.Port, _connectionDetails.UserName, authMethods.ToArray())
        {
            Timeout = TimeSpan.FromSeconds(30),
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        while (_availableClients.TryDequeue(out var client))
        {
            client?.Dispose();
        }

        _semaphore?.Dispose();
        _disposed = true;
    }
}