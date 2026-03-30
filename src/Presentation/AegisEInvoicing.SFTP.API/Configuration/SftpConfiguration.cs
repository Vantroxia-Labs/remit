using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.SFTP.API.Configuration;

/// <summary>
/// Configuration for SFTP connections and processing settings
/// </summary>
public class SftpConfiguration
{
    public const string SectionName = "SFTPDetails";
    
    /// <summary>
    /// Default SFTP server hostname or IP address (from database connections)
    /// </summary>
    [Required]
    public string Host { get; set; } = string.Empty;
    
    /// <summary>
    /// Default SFTP server port
    /// </summary>
    public int Port { get; set; } = 22;
    
    /// <summary>
    /// Default directory for NACK (Negative Acknowledgment/Rejected) files
    /// </summary>
    public string RejectedDirectory { get; set; } = "Rejected";

    /// <summary>
    /// Default directory for ACK (Acknowledgment/Receipts) files
    /// </summary>
    public string ReceiptsDirectory { get; set; } = "Receipts";

    /// <summary>
    /// Default directory where clients drop files to be processed (inbox)
    /// </summary>
    public string PendingDirectory { get; set; } = "Pending";

    /// <summary>
    /// Default directory for files currently being processed (prevents reprocessing on crash)
    /// </summary>
    public string InProgressDirectory { get; set; } = "In-Progress";
    
    /// <summary>
    /// Default file pattern to match XML files
    /// </summary>
    public string FilePattern { get; set; } = "*.xml";
    
    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Operation timeout in seconds
    /// </summary>
    public int OperationTimeoutSeconds { get; set; } = 60;
    
    /// <summary>
    /// Maximum retry attempts for failed operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;
    
    /// <summary>
    /// Maximum concurrent SFTP connections
    /// </summary>
    public int MaxConcurrentConnections { get; set; } = 5;
    
    /// <summary>
    /// File processing batch size
    /// </summary>
    public int FileBatchSize { get; set; } = 10;
}

/// <summary>
/// Individual SFTP connection configuration
/// </summary>
public class SftpConnectionDetails
{
    /// <summary>
    /// Unique identifier for this SFTP connection
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// SFTP server hostname or IP address
    /// </summary>
    [Required]
    public string Host { get; set; } = string.Empty;
    
    /// <summary>
    /// Username for SFTP authentication
    /// </summary>
    [Required]
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// Password for SFTP authentication (consider using secure storage)
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// SFTP server port
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 22;
    
    /// <summary>
    /// Working directory on the SFTP server (base directory for Pending/In-Progress/etc)
    /// </summary>
    [Required]
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Directory for NACK (Negative Acknowledgment/Rejected) files
    /// </summary>
    public string RejectedDirectory { get; set; } = "Rejected";

    /// <summary>
    /// Directory for ACK (Acknowledgment/Receipts) files
    /// </summary>
    public string ReceiptsDirectory { get; set; } = "Receipts";

    /// <summary>
    /// Directory where clients drop files to be processed (inbox)
    /// </summary>
    public string PendingDirectory { get; set; } = "Pending";

    /// <summary>
    /// Directory for files currently being processed (prevents reprocessing on crash)
    /// </summary>
    public string InProgressDirectory { get; set; } = "In-Progress";
    
    /// <summary>
    /// File pattern to match XML files (default: *.xml)
    /// </summary>
    public string FilePattern { get; set; } = "*.xml";
    
    /// <summary>
    /// Whether this connection is enabled for processing
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Business ID associated with this SFTP connection (if applicable)
    /// </summary>
    public Guid? BusinessId { get; set; }
    
    /// <summary>
    /// Optional description for this connection
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Key-based authentication file path (alternative to password)
    /// </summary>
    public string? PrivateKeyFilePath { get; set; }
    
    /// <summary>
    /// Passphrase for the private key (if required)
    /// </summary>
    public string? PrivateKeyPassphrase { get; set; }
}

/// <summary>
/// Processing configuration for the background service
/// </summary>
public class ProcessingConfiguration
{
    public const string SectionName = "Processing";

    /// <summary>
    /// How often the service should check for new files (in seconds)
    /// </summary>
    public int ProcessingIntervalSeconds { get; set; } = 30;
    
    /// <summary>
    /// Maximum number of files to process in a single batch
    /// </summary>
    public int MaxFilesPerBatch { get; set; } = 50;
    
    /// <summary>
    /// Whether to delete source files after successful processing
    /// </summary>
    public bool DeleteSourceFilesAfterProcessing { get; set; } = false;
    
    /// <summary>
    /// Maximum file size allowed (in MB)
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 10;
    
    /// <summary>
    /// Whether to enable parallel processing of files
    /// </summary>
    public bool EnableParallelProcessing { get; set; } = true;
    
    /// <summary>
    /// Maximum degree of parallelism
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    
    /// <summary>
    /// Whether to process files in a distributed manner (for multiple service instances)
    /// </summary>
    public bool EnableDistributedProcessing { get; set; } = false;
    
    /// <summary>
    /// Lock timeout for distributed processing (in minutes)
    /// </summary>
    public int DistributedLockTimeoutMinutes { get; set; } = 10;
}

/// <summary>
/// Email notification configuration
/// </summary>
public class NotificationConfiguration
{
    public const string SectionName = "Notification";
    
    /// <summary>
    /// Whether email notifications are enabled
    /// </summary>
    public bool EnableEmailNotifications { get; set; } = true;
    
    /// <summary>
    /// Email template for successful invoice processing
    /// </summary>
    public string SuccessEmailTemplate { get; set; } = "invoice-success";
    
    /// <summary>
    /// Email template for failed invoice processing
    /// </summary>
    public string ErrorEmailTemplate { get; set; } = "invoice-error";
    
    /// <summary>
    /// Whether to send notifications for every processed invoice
    /// </summary>
    public bool SendNotificationForEachInvoice { get; set; } = true;
    
    /// <summary>
    /// Whether to send summary notifications
    /// </summary>
    public bool SendSummaryNotifications { get; set; } = true;
    
    /// <summary>
    /// Summary notification frequency (in hours)
    /// </summary>
    public int SummaryNotificationFrequencyHours { get; set; } = 24;
    
    /// <summary>
    /// List of email recipients for successful invoice notifications
    /// </summary>
    public List<string> SuccessNotificationRecipients { get; set; } = new();
    
    /// <summary>
    /// List of email recipients for error notifications
    /// </summary>
    public List<string> ErrorNotificationRecipients { get; set; } = new();
    
    /// <summary>
    /// List of email recipients for summary notifications
    /// </summary>
    public List<string> SummaryNotificationRecipients { get; set; } = new();
    
    /// <summary>
    /// Subject line for successful invoice notifications
    /// </summary>
    public string SuccessEmailSubject { get; set; } = "Invoice Successfully Processed - {InvoiceId}";
    
    /// <summary>
    /// Subject line for error notifications
    /// </summary>
    public string ErrorEmailSubject { get; set; } = "Invoice Processing Failed - {FileName}";
    
    /// <summary>
    /// Subject line for summary notifications
    /// </summary>
    public string SummaryEmailSubject { get; set; } = "Daily Invoice Processing Summary";
}
