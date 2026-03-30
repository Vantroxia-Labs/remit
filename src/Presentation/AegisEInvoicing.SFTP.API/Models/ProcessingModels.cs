namespace AegisEInvoicing.SFTP.API.Models;

/// <summary>
/// Represents a file processing result
/// </summary>
public class FileProcessingResult
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingDuration { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? PartyId { get; set; }
    public string? IRN { get; set; }
    public ProcessingAction Action { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional metadata about the processing
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    public static FileProcessingResult Success(
        string fileName, 
        string filePath, 
        string connectionId,
        Guid invoiceId, 
        Guid partyId, 
        string irn, 
        TimeSpan duration)
    {
        return new FileProcessingResult
        {
            FileName = fileName,
            FilePath = filePath,
            ConnectionId = connectionId,
            IsSuccess = true,
            InvoiceId = invoiceId,
            PartyId = partyId,
            IRN = irn,
            ProcessingDuration = duration,
            Action = ProcessingAction.Success
        };
    }
    
    public static FileProcessingResult Error(
        string fileName, 
        string filePath, 
        string connectionId,
        string errorMessage, 
        Exception? exception = null,
        TimeSpan duration = default)
    {
        return new FileProcessingResult
        {
            FileName = fileName,
            FilePath = filePath,
            ConnectionId = connectionId,
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            ProcessingDuration = duration,
            Action = ProcessingAction.Error
        };
    }
}

/// <summary>
/// Represents the action taken after processing a file
/// </summary>
public enum ProcessingAction
{
    Success,
    Error,
    Skipped,
    Retry
}

/// <summary>
/// Represents an SFTP file to be processed
/// </summary>
public class SftpFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public string Extension => Path.GetExtension(FileName);
    public bool IsXmlFile => Extension.Equals(".xml", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Represents an XML acknowledgment or negative acknowledgment response
/// </summary>
public class XmlResponse
{
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public XmlResponseType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string TargetDirectory { get; set; } = string.Empty;
    
    /// <summary>
    /// Invoice details for ACK responses
    /// </summary>
    public InvoiceDetails? Invoice { get; set; }
    
    /// <summary>
    /// Error details for NACK responses
    /// </summary>
    public ErrorDetails? Error { get; set; }
}

/// <summary>
/// Type of XML response
/// </summary>
public enum XmlResponseType
{
    /// <summary>
    /// Acknowledgment - successful processing
    /// </summary>
    ACK,
    
    /// <summary>
    /// Negative Acknowledgment - error in processing
    /// </summary>
    NACK
}

/// <summary>
/// Invoice details for successful processing
/// </summary>
public class InvoiceDetails
{
    public Guid InvoiceId { get; set; }
    public Guid PartyId { get; set; }
    public string IRN { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public string BusinessId { get; set; } = string.Empty;
    public decimal? TotalAmount { get; set; }
    public string? Currency { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? IssueDate { get; set; }
}

/// <summary>
/// Error details for failed processing
/// </summary>
public class ErrorDetails
{
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public DateTime ErrorOccurredAt { get; set; } = DateTime.UtcNow;
    public string? ValidationErrors { get; set; }
    public string ErrorSeverity { get; set; } = "High";
}

/// <summary>
/// Represents processing statistics
/// </summary>
public class ProcessingStatistics
{
    public int TotalFilesProcessed { get; set; }
    public int SuccessfulFiles { get; set; }
    public int ErrorFiles { get; set; }
    public int SkippedFiles { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public TimeSpan AverageProcessingTimePerFile => TotalFilesProcessed > 0 
        ? TimeSpan.FromMilliseconds(TotalProcessingTime.TotalMilliseconds / TotalFilesProcessed) 
        : TimeSpan.Zero;
    public DateTime ProcessingStartTime { get; set; }
    public DateTime ProcessingEndTime { get; set; }
    public List<string> ErrorSummary { get; set; } = new();
    public Dictionary<string, int> ConnectionStats { get; set; } = new();
    
    public double SuccessRate => TotalFilesProcessed > 0 
        ? (double)SuccessfulFiles / TotalFilesProcessed * 100 
        : 0;
}

/// <summary>
/// Represents the current status of the processing service
/// </summary>
public class ServiceStatus
{
    public bool IsRunning { get; set; }
    public DateTime LastProcessingRun { get; set; }
    public DateTime ServiceStartTime { get; set; }
    public ProcessingStatistics CurrentBatchStatistics { get; set; } = new();
    public List<string> ActiveConnections { get; set; } = new();
    public List<string> HealthMessages { get; set; } = new();
    public ServiceHealth Health { get; set; } = ServiceHealth.Healthy;
}

/// <summary>
/// Service health status
/// </summary>
public enum ServiceHealth
{
    Healthy,
    Warning,
    Critical,
    Unknown
}

/// <summary>
/// Distributed lock information
/// </summary>
public class ProcessingLock
{
    public string LockId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public string ProcessingInstance { get; set; } = Environment.MachineName;
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}