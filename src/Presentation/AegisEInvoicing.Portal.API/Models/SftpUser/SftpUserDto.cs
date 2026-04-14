using AegisEInvoicing.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.SftpUser;

/// <summary>
/// SFTP User response DTO
/// </summary>
public class SftpUserDto
{
    public Guid Id { get; set; }
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public SFTPUserStatus Status { get; set; }
    public string RootDirectoryPath { get; set; } = null!;
    public string WorkingDirectory { get; set; } = null!;
    public bool DirectoriesCreated { get; set; }
    public DateTimeOffset? SFTPGoCreatedAt { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Request to change SFTP user password
/// </summary>
public class ChangeSftpPasswordRequest
{
    public string Username { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

/// <summary>
/// Request to rename SFTP user
/// </summary>
public class RenameSftpUserRequest
{
    public string Username { get; set; } = null!;
    public string NewUsername { get; set; } = null!;
}

/// <summary>
/// Response for SFTP operations
/// </summary>
public class SftpOperationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
}

/// <summary>
/// Request to get user information from SFTPGo
/// </summary>
public class GetSftpUserInfoRequest
{
    public string Username { get; set; } = null!;
}


/// <summary>
/// Server summary response
/// </summary>
public class ServerSummaryResponse
{
    public bool IsSslEnabled { get; set; }
    public string? SslKeyType { get; set; }
    public uint SslKeyBits { get; set; }
    public bool IsSoapWebEnabled { get; set; }
    public bool IsSoapSecure { get; set; }
    public uint SoapPort { get; set; }
    public string? IpPublic { get; set; }
    public string FtpStatus { get; set; } = string.Empty;
    public string SftpStatus { get; set; } = string.Empty;
    public string HttpStatus { get; set; } = string.Empty;
    public bool HipaaCompliant { get; set; }
}

/// <summary>
/// Current status response
/// </summary>
public class CurrentStatusResponse
{
    public bool IsStarted { get; set; }
    public ServerStatistics? Stats { get; set; }
    public double DownBandwidth { get; set; }
    public double UpBandwidth { get; set; }
    public ulong TotalConnections { get; set; }
}

/// <summary>
/// Server statistics
/// </summary>
public class ServerStatistics
{
    public ulong TotalBytesTransferred { get; set; }
    public ulong SessionsCreated { get; set; }
    public ulong FilesUploaded { get; set; }
    public ulong FilesDownloaded { get; set; }
}

/// <summary>
/// Log message response
/// </summary>
public class LogMessageResponse
{
    public ulong Id { get; set; }
    public int Type { get; set; }
    public DateTime Time { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// File transfer response
/// </summary>
public class FileTransferResponse
{
    public ulong ID { get; set; }
    public string? LocalFilename { get; set; }
    public string? RemoteFilename { get; set; }
    public string? User { get; set; }
    public ulong PercentElapsed { get; set; }
    public ulong CurrentPosition { get; set; }
    public ulong TotalSize { get; set; }
    public double TransferRate { get; set; }
    public string? TimeLeft { get; set; }
    public string? Type { get; set; }
    public bool IsSecure { get; set; }
}

/// <summary>
/// Request for getting log messages
/// </summary>
public class GetLogMessagesRequest
{
    public ulong? StartMessageId { get; set; }
    public int? MaxMessages { get; set; }
}

/// <summary>
/// Admin accounts request/response
/// </summary>
public class AdminAccountsRequest
{
    public string Data { get; set; } = null!;
}

/// <summary>
/// Admin accounts response
/// </summary>
public class AdminAccountsResponse
{
    public bool Result { get; set; }
    public object? Data { get; set; }
}

// Business-specific SFTP DTOs

/// <summary>
/// Request to change SFTP password for business clients
/// </summary>
public class BusinessChangeSftpPasswordRequest
{
    [Required]
    public string OldPassword { get; set; } = null!;
    [Required]
    public string NewPassword { get; set; } = null!;
}

/// <summary>
/// Individual service check result
/// </summary>
public class ServiceCheck
{
    public string CheckName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Details { get; set; } = null!;
}
