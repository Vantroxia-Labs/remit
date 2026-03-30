using AegisEInvoicing.SFTP.API.Configuration;
using AegisEInvoicing.SFTP.API.Models;
using AegisEInvoicing.SFTP.API.Services.Interfaces;

namespace AegisEInvoicing.SFTP.API.Services;

/// <summary>
/// File system-based SFTP service for local SFTPGo installations
/// Reads files directly from disk instead of connecting via SFTP
/// </summary>
public class LocalFileSystemSftpService(
    IDatabaseSftpService databaseSftpService,
    IConfiguration configuration,
    ILogger<LocalFileSystemSftpService> logger) : ISftpService
{
    private readonly IDatabaseSftpService _databaseSftpService = databaseSftpService;
    private readonly ILogger<LocalFileSystemSftpService> _logger = logger;
    private readonly string _ftpRootPath = configuration["SftpConfiguration:FtpRootPath"] ?? "C:/ftproot";

    public async Task<List<SftpFileInfo>> ListInvoiceFilesAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken);
        if (connectionDetails == null || !connectionDetails.BusinessId.HasValue)
        {
            _logger.LogWarning("Connection or BusinessId not found for {ConnectionId}", connectionId);
            return [];
        }

        var businessRoot = Path.Combine(_ftpRootPath, "uploads", connectionDetails.BusinessId.Value.ToString());
        var pendingPath = Path.Combine(businessRoot, "Pending");
        var inProgressPath = Path.Combine(businessRoot, "In-Progress");

        if (!Directory.Exists(pendingPath))
        {
            _logger.LogDebug("Pending directory does not exist: {Path}", pendingPath);
            return [];
        }

        Directory.CreateDirectory(inProgressPath);

        var files = new List<SftpFileInfo>();
        var invoiceFiles = Directory.GetFiles(pendingPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var filePath in invoiceFiles)
        {
            var fileInfo = new FileInfo(filePath);
            var destPath = Path.Combine(inProgressPath, fileInfo.Name);

            // Handle duplicate filenames in In-Progress
            if (File.Exists(destPath))
            {
                var baseName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                var ext = Path.GetExtension(fileInfo.Name);
                var version = 2;
                while (File.Exists(destPath))
                {
                    destPath = Path.Combine(inProgressPath, $"{baseName}_V{version}{ext}");
                    version++;
                }
            }

            // Atomic move: Pending → In-Progress before returning
            File.Move(filePath, destPath);
            _logger.LogDebug("Moved {FileName} from Pending to In-Progress", fileInfo.Name);

            files.Add(new SftpFileInfo
            {
                FileName = Path.GetFileName(destPath),
                FullPath = destPath.Replace('\\', '/'),
                Size = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime,
                ConnectionId = connectionId
            });
        }

        _logger.LogInformation("Found {Count} invoice file(s) in Pending for connection {ConnectionId}, moved to In-Progress",
            files.Count, connectionId);
        return files;
    }

    public async Task<List<SftpFileInfo>> ListInProgressFilesAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        var connectionDetails = await GetConnectionDetailsAsync(connectionId, cancellationToken);
        if (connectionDetails == null || !connectionDetails.BusinessId.HasValue)
        {
            _logger.LogWarning("Connection or BusinessId not found for {ConnectionId}", connectionId);
            return [];
        }

        // Direct filesystem path: C:/ftproot/uploads/{businessId}/In-Progress
        var directoryPath = Path.Combine(_ftpRootPath, "uploads", connectionDetails.BusinessId.Value.ToString(), "In-Progress");

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogDebug("Directory does not exist: {Path}", directoryPath);
            return new List<SftpFileInfo>();
        }

        var files = new List<SftpFileInfo>();
        var invoiceFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var filePath in invoiceFiles)
        {
            var fileInfo = new FileInfo(filePath);
            files.Add(new SftpFileInfo
            {
                FileName = fileInfo.Name,
                FullPath = filePath.Replace('\\', '/'),
                Size = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime,
                ConnectionId = connectionId
            });
        }

        _logger.LogInformation("Found {Count} invoice files (XML/JSON) in {Directory}", files.Count, directoryPath);
        return files;
    }

    public async Task<string> DownloadFileContentAsync(string connectionId, string remoteFilePath, CancellationToken cancellationToken = default)
    {
        var localPath = ConvertToLocalPath(remoteFilePath);
        return await File.ReadAllTextAsync(localPath, cancellationToken);
    }

    public async Task<MemoryStream> DownloadFileStreamAsync(string connectionId, string remoteFilePath, CancellationToken cancellationToken = default)
    {
        var localPath = ConvertToLocalPath(remoteFilePath);
        var bytes = await File.ReadAllBytesAsync(localPath, cancellationToken);
        return new MemoryStream(bytes);
    }

    public async Task UploadFileAsync(string connectionId, string remoteFilePath, string content, CancellationToken cancellationToken = default)
    {
        var localPath = ConvertToLocalPath(remoteFilePath);
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await File.WriteAllTextAsync(localPath, content, cancellationToken);
    }

    public async Task UploadFileAsync(string connectionId, string remoteFilePath, Stream contentStream, CancellationToken cancellationToken = default)
    {
        var localPath = ConvertToLocalPath(remoteFilePath);
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        using var fileStream = File.Create(localPath);
        await contentStream.CopyToAsync(fileStream, cancellationToken);
    }

    public Task MoveFileAsync(string connectionId, string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken = default)
    {
        var sourcePath = ConvertToLocalPath(sourceFilePath);
        var destPath = ConvertToLocalPath(destinationFilePath);
        
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        
        // Handle duplicate filenames
        if (File.Exists(destPath))
        {
            var directory = Path.GetDirectoryName(destPath)!;
            var baseName = Path.GetFileNameWithoutExtension(destPath);
            var extension = Path.GetExtension(destPath);
            var version = 2;
            
            while (File.Exists(destPath))
            {
                destPath = Path.Combine(directory, $"{baseName}_V{version}{extension}");
                version++;
            }
        }
        
        File.Move(sourcePath, destPath);
        _logger.LogInformation("Moved file from {Source} to {Dest}", sourcePath, destPath);
        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string connectionId, string remoteFilePath, CancellationToken cancellationToken = default)
    {
        var localPath = ConvertToLocalPath(remoteFilePath);
        if (File.Exists(localPath))
        {
            File.Delete(localPath);
        }
        return Task.CompletedTask;
    }

    public Task CreateDirectoryIfNotExistsAsync(string connectionId, string remoteDirectoryPath, CancellationToken cancellationToken = default)
    {
        var localPath = ConvertToLocalPath(remoteDirectoryPath);
        Directory.CreateDirectory(localPath);
        return Task.CompletedTask;
    }

    public Task<bool> TestConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        // Always return true for local filesystem
        return Task.FromResult(true);
    }

    public async Task<Dictionary<string, bool>> TestAllConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var connections = await GetEnabledConnectionsAsync(cancellationToken);
        return connections.ToDictionary(c => c.ConnectionId, c => true);
    }

    public Task<SftpConnectionDetails?> GetConnectionDetailsAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        return _databaseSftpService.GetSftpConnectionAsync(connectionId);
    }

    public Task<List<SftpConnectionDetails>> GetAllConnectionsAsync(CancellationToken cancellationToken = default)
    {
        return _databaseSftpService.GetEnabledSftpConnectionsAsync();
    }

    public Task<List<SftpConnectionDetails>> GetEnabledConnectionsAsync(CancellationToken cancellationToken = default)
    {
        return _databaseSftpService.GetEnabledSftpConnectionsAsync();
    }

    private string ConvertToLocalPath(string remotePath)
    {
        // Convert /uploads/businessId/folder/file.xml to C:/ftproot/uploads/businessId/folder/file.xml
        var relativePath = remotePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_ftpRootPath, relativePath);
    }
}
