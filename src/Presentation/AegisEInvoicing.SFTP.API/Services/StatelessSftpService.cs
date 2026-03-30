using AegisEInvoicing.SFTP.API.Configuration;
using AegisEInvoicing.SFTP.API.Models;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System.Text;

namespace AegisEInvoicing.SFTP.API.Services;

/// <summary>
/// Completely stateless SFTP service that creates fresh connections for every operation
/// This eliminates ALL caching issues by never reusing connections or maintaining any state
/// </summary>
public class StatelessSftpService(
    IOptions<SftpConfiguration> sftpConfig,
    IDatabaseSftpService databaseSftpService,
    ILogger<StatelessSftpService> logger) : ISftpService, IDisposable
{
    private readonly SftpConfiguration _sftpConfig = sftpConfig.Value ?? throw new ArgumentNullException(nameof(sftpConfig));
    private readonly IDatabaseSftpService _databaseSftpService = databaseSftpService ?? throw new ArgumentNullException(nameof(databaseSftpService));
    private readonly ILogger<StatelessSftpService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private bool _disposed;

    public async Task<List<SftpFileInfo>> ListInvoiceFilesAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("[STATELESS-{OperationId}] Starting fresh invoice file listing (XML/JSON) for connection: {ConnectionId}", operationId, connectionId);

        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false) ?? throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        var files = new List<SftpFileInfo>();
        SftpClient? client = null;

        try
        {
            // Create completely fresh connection with no pooling
            client = await CreateFreshConnectionAsync(connectionDetails, operationId, cancellationToken);

            // Build Pending directory path
            var baseDirectory = Path.GetDirectoryName(connectionDetails.WorkingDirectory)?.Replace('\\', '/') ?? "/";
            var pendingDir = Path.Combine(baseDirectory, connectionDetails.PendingDirectory).Replace('\\', '/');
            var inProgressDir = Path.Combine(baseDirectory, connectionDetails.InProgressDirectory).Replace('\\', '/');

            _logger.LogDebug("[STATELESS-{OperationId}] Performing stateless directory listing for Pending: {Directory}", 
                operationId, pendingDir);

            // Get directory listing with multiple strategies to bypass any server-side caching
            var allFiles = await GetDirectoryListingWithMultipleStrategies(client, pendingDir, operationId, cancellationToken);

            // Filter for XML and JSON files
            var invoiceFiles = allFiles.Where(f => f.IsRegularFile && 
                (f.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                 f.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))).ToList();

            _logger.LogInformation("[STATELESS-{OperationId}] Found {InvoiceCount} invoice files (XML/JSON) out of {TotalCount} files", 
                operationId, invoiceFiles.Count, allFiles.Count);

            // Atomically move Pending → In-Progress
            foreach (var file in invoiceFiles)
            {
                try
                {
                    var sourceFilePath = file.FullName;
                    var destFilePath = Path.Combine(inProgressDir, file.Name).Replace('\\', '/');

                    // Ensure In-Progress directory exists
                    if (!client.Exists(inProgressDir))
                    {
                        client.CreateDirectory(inProgressDir);
                    }

                    // Handle duplicate filenames
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
                    _logger.LogDebug("[STATELESS-{OperationId}] Moved {FileName} from Pending to In-Progress", operationId, file.Name);

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
                    _logger.LogError(ex, "[STATELESS-{OperationId}] Failed to move {FileName} from Pending to In-Progress", operationId, file.Name);
                }
            }

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STATELESS-{OperationId}] Error listing invoice files: {Error}", operationId, ex.Message);
            throw;
        }
        finally
        {
            // Always dispose connection immediately - no pooling
            client?.Dispose();
            _logger.LogDebug("[STATELESS-{OperationId}] Connection disposed", operationId);
        }
    }

    public async Task<List<SftpFileInfo>> ListInProgressFilesAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("[STATELESS-{OperationId}] Starting fresh In-Progress invoice file listing for connection: {ConnectionId}", operationId, connectionId);

        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false) ?? throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        var files = new List<SftpFileInfo>();
        SftpClient? client = null;

        try
        {
            // Create completely fresh connection with no pooling
            client = await CreateFreshConnectionAsync(connectionDetails, operationId, cancellationToken);

            // Build In-Progress directory path
            var baseDirectory = Path.GetDirectoryName(connectionDetails.WorkingDirectory)?.Replace('\\', '/') ?? "/";
            var inProgressDir = Path.Combine(baseDirectory, connectionDetails.InProgressDirectory).Replace('\\', '/');

            _logger.LogDebug("[STATELESS-{OperationId}] Performing stateless directory listing for In-Progress: {Directory}", 
                operationId, inProgressDir);

            // Get directory listing with multiple strategies to bypass any server-side caching
            var allFiles = await GetDirectoryListingWithMultipleStrategies(client, inProgressDir, operationId, cancellationToken);

            // Filter for XML and JSON files
            var invoiceFiles = allFiles.Where(f => f.IsRegularFile && 
                (f.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                 f.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))).ToList();

            _logger.LogInformation("[STATELESS-{OperationId}] Found {InvoiceCount} invoice files (XML/JSON) out of {TotalCount} files", 
                operationId, invoiceFiles.Count, allFiles.Count);

            // Convert to our model
            foreach (var invoiceFile in invoiceFiles)
            {
                files.Add(new SftpFileInfo
                {
                    FileName = invoiceFile.Name,
                    FullPath = invoiceFile.FullName,
                    Size = invoiceFile.Length,
                    LastModified = invoiceFile.LastWriteTime,
                    ConnectionId = connectionId
                });

                _logger.LogDebug("[STATELESS-{OperationId}] Invoice file: {FileName} ({Size} bytes, Modified: {LastModified})", 
                    operationId, invoiceFile.Name, invoiceFile.Length, invoiceFile.LastWriteTime);
            }

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STATELESS-{OperationId}] Error listing In-Progress invoice files: {Error}", operationId, ex.Message);
            throw;
        }
        finally
        {
            // Always dispose connection immediately - no pooling
            client?.Dispose();
            _logger.LogDebug("[STATELESS-{OperationId}] Connection disposed", operationId);
        }
    }

    private async Task<SftpClient> CreateFreshConnectionAsync(SftpConnectionDetails connectionDetails, string operationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("[STATELESS-{OperationId}] Creating completely fresh SFTP connection to {Host}:{Port}", 
            operationId, connectionDetails.Host, connectionDetails.Port);

        var authMethods = new List<AuthenticationMethod>();

        if (!string.IsNullOrEmpty(connectionDetails.Password))
        {
            authMethods.Add(new PasswordAuthenticationMethod(connectionDetails.UserName, connectionDetails.Password));
        }

        if (!string.IsNullOrEmpty(connectionDetails.PrivateKeyFilePath) && File.Exists(connectionDetails.PrivateKeyFilePath))
        {
            var keyFile = string.IsNullOrEmpty(connectionDetails.PrivateKeyPassphrase)
                ? new PrivateKeyFile(connectionDetails.PrivateKeyFilePath)
                : new PrivateKeyFile(connectionDetails.PrivateKeyFilePath, connectionDetails.PrivateKeyPassphrase);

            authMethods.Add(new PrivateKeyAuthenticationMethod(connectionDetails.UserName, keyFile));
        }

        if (authMethods.Count == 0)
        {
            throw new InvalidOperationException($"No authentication method configured for connection: {connectionDetails.ConnectionId}");
        }

        var connectionInfo = new Renci.SshNet.ConnectionInfo(connectionDetails.Host, connectionDetails.Port, connectionDetails.UserName, authMethods.ToArray())
        {
            Timeout = TimeSpan.FromSeconds(30),
        };

        var client = new SftpClient(connectionInfo);
        client.OperationTimeout = TimeSpan.FromSeconds(60);

        _logger.LogDebug("[STATELESS-{OperationId}] Connecting to SFTP server...", operationId);
        
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30));




        await Task.Run(() => client.Connect(), cts.Token);

        _logger.LogDebug("[STATELESS-{OperationId}] Connection established successfully", operationId);

        // Verify connection with a simple test
        await Task.Run(() => client.ListDirectory(".").Take(1).ToList(), cancellationToken);

        return client;
    }

    private async Task<List<ISftpFile>> GetDirectoryListingWithMultipleStrategies(SftpClient client, string directory, string operationId, CancellationToken cancellationToken)
    {
        var allFiles = new List<ISftpFile>();

        _logger.LogDebug("[STATELESS-{OperationId}] Using multiple strategies to get fresh directory listing", operationId);

        // Strategy 1: Direct directory listing
        try
        {
            _logger.LogDebug("[STATELESS-{OperationId}] Strategy 1: Direct listing of {Directory}", operationId, directory);
            var files1 = await Task.Run(() => client.ListDirectory(directory).Where(f => f.IsRegularFile).ToList(), cancellationToken);
            _logger.LogDebug("[STATELESS-{OperationId}] Strategy 1 found {Count} files", operationId, files1.Count);
            allFiles.AddRange(files1);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[STATELESS-{OperationId}] Strategy 1 failed: {Error}", operationId, ex.Message);
        }

        // Strategy 2: Change directory and list current
        try
        {
            _logger.LogDebug("[STATELESS-{OperationId}] Strategy 2: Change to directory and list current", operationId);
            var originalDir = client.WorkingDirectory;
            client.ChangeDirectory(directory);
            var files2 = await Task.Run(() => client.ListDirectory(".").Where(f => f.IsRegularFile).ToList(), cancellationToken);
            client.ChangeDirectory(originalDir);
            _logger.LogDebug("[STATELESS-{OperationId}] Strategy 2 found {Count} files", operationId, files2.Count);
            
            // Merge unique files
            foreach (var file in files2)
            {
                if (!allFiles.Any(f => f.FullName == file.FullName))
                {
                    allFiles.Add(file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[STATELESS-{OperationId}] Strategy 2 failed: {Error}", operationId, ex.Message);
        }

        // Strategy 3: Multiple path variants
        var pathVariants = new[]
        {
            directory,
            directory.TrimEnd('/') + "/",
            directory.TrimEnd('/')
        };

        foreach (var pathVariant in pathVariants.Distinct())
        {
            try
            {
                _logger.LogDebug("[STATELESS-{OperationId}] Strategy 3: Trying path variant '{Path}'", operationId, pathVariant);
                var files3 = await Task.Run(() => client.ListDirectory(pathVariant).Where(f => f.IsRegularFile).ToList(), cancellationToken);
                _logger.LogDebug("[STATELESS-{OperationId}] Path variant '{Path}' found {Count} files", operationId, pathVariant, files3.Count);
                
                // Merge unique files
                foreach (var file in files3)
                {
                    if (!allFiles.Any(f => f.FullName == file.FullName))
                    {
                        allFiles.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[STATELESS-{OperationId}] Path variant '{Path}' failed: {Error}", operationId, pathVariant, ex.Message);
            }
        }

        _logger.LogInformation("[STATELESS-{OperationId}] Combined strategies found {TotalFiles} unique files", operationId, allFiles.Count);
        return allFiles;
    }

    // Implement other required interface methods with stateless approach
    public async Task<string> DownloadFileContentAsync(string connectionId, string remoteFilePath, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogDebug("[STATELESS-{OperationId}] Downloading file content: {FilePath}", operationId, remoteFilePath);

        using var stream = await DownloadFileStreamAsync(connectionId, remoteFilePath, cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    public async Task<MemoryStream> DownloadFileStreamAsync(string connectionId, string remoteFilePath, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (connectionDetails == null)
        {
            throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        }

        SftpClient? client = null;
        try
        {
            client = await CreateFreshConnectionAsync(connectionDetails, operationId, cancellationToken);
            var memoryStream = new MemoryStream();
            client.DownloadFile(remoteFilePath, memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
        finally
        {
            client?.Dispose();
        }
    }

    public async Task UploadFileAsync(string connectionId, string remoteFilePath, string content, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await UploadFileAsync(connectionId, remoteFilePath, stream, cancellationToken);
    }

    public async Task UploadFileAsync(string connectionId, string remoteFilePath, Stream contentStream, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (connectionDetails == null)
        {
            throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        }

        SftpClient? client = null;
        try
        {
            client = await CreateFreshConnectionAsync(connectionDetails, operationId, cancellationToken);
            contentStream.Position = 0;
            client.UploadFile(contentStream, remoteFilePath);
        }
        finally
        {
            client?.Dispose();
        }
    }

    public async Task MoveFileAsync(string connectionId, string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (connectionDetails == null)
        {
            throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        }

        SftpClient? client = null;
        try
        {
            client = await CreateFreshConnectionAsync(connectionDetails, operationId, cancellationToken);
            
            if (!client.Exists(sourceFilePath))
            {
                throw new FileNotFoundException($"Source file does not exist: {sourceFilePath}");
            }

            // If a file with the same name already exists in the processed directory,
            // append a version suffix (e.g. _V2, _V3, ...) before moving.
            var finalDestination = destinationFilePath;
            if (client.Exists(destinationFilePath))
            {
                var directory = Path.GetDirectoryName(destinationFilePath)?.Replace('\\', '/') ?? "/";
                var baseName = Path.GetFileNameWithoutExtension(destinationFilePath);
                var extension = Path.GetExtension(destinationFilePath);

                var version = 2;
                while (true)
                {
                    var candidateName = $"{baseName}_V{version}{extension}";
                    var candidatePath = Path.Combine(directory, candidateName).Replace('\\', '/');

                    if (!client.Exists(candidatePath))
                    {
                        finalDestination = candidatePath;
                        _logger.LogInformation(
                            "[STATELESS-{OperationId}] Destination file already exists. Using versioned path {Path}",
                            operationId, finalDestination);
                        break;
                    }

                    version++;
                }
            }

            client.RenameFile(sourceFilePath, finalDestination);
            
            // Verify the move
            await Task.Delay(200, cancellationToken);
            if (!client.Exists(finalDestination) || client.Exists(sourceFilePath))
            {
                throw new InvalidOperationException("File move verification failed");
            }
        }
        finally
        {
            client?.Dispose();
        }
    }

    public async Task DeleteFileAsync(string connectionId, string remoteFilePath, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (connectionDetails == null)
        {
            throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        }

        SftpClient? client = null;
        try
        {
            client = await CreateFreshConnectionAsync(connectionDetails, operationId, cancellationToken);
            client.DeleteFile(remoteFilePath);
        }
        finally
        {
            client?.Dispose();
        }
    }

    public async Task CreateDirectoryIfNotExistsAsync(string connectionId, string remoteDirectoryPath, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (connectionDetails == null)
        {
            throw new ArgumentException($"Connection '{connectionId}' not found", nameof(connectionId));
        }

        SftpClient? client = null;
        try
        {
            client = await CreateFreshConnectionAsync(connectionDetails, operationId, cancellationToken);
            
            if (!client.Exists(remoteDirectoryPath))
            {
                client.CreateDirectory(remoteDirectoryPath);
            }
        }
        finally
        {
            client?.Dispose();
        }
    }

    public async Task<bool> TestConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        try
        {
            var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken).ConfigureAwait(false);
            if (connectionDetails == null)
            {
                return false;
            }

            SftpClient? client = null;
            try
            {
                client = await CreateFreshConnectionAsync(connectionDetails, operationId, cancellationToken);
                return client.Exists(connectionDetails.WorkingDirectory);
            }
            finally
            {
                client?.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STATELESS-{OperationId}] Connection test failed: {Error}", operationId, ex.Message);
            return false;
        }
    }

    public async Task<Dictionary<string, bool>> TestAllConnectionsAsync(CancellationToken cancellationToken = default)
    {
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Nothing to dispose - we're completely stateless
    }
}