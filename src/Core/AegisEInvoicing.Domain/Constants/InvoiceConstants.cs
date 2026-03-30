namespace AegisEInvoicing.Domain.Constants;

/// <summary>
/// Constants for invoice processing and validation
/// </summary>
public static class InvoiceConstants
{
    /// <summary>
    /// Number of invoices to process in a single batch during bulk upload
    /// </summary>
    public const int UPLOAD_BATCH_SIZE = 100;

    /// <summary>
    /// Required length for FIRS service ID
    /// </summary>
    public const int FIRS_SERVICE_ID_LENGTH = 8;

    /// <summary>
    /// Maximum file size for invoice uploads (500MB)
    /// </summary>
    public const long MAX_UPLOAD_FILE_SIZE = 500_000_000;

    /// <summary>
    /// Number of rows to process in a single batch when streaming Excel files
    /// </summary>
    public const int EXCEL_BATCH_SIZE = 1000;

    /// <summary>
    /// Default currency code for invoices
    /// </summary>
    public const string DEFAULT_CURRENCY = "NGN";

    /// <summary>
    /// Threshold for forcing garbage collection during large batch operations
    /// </summary>
    public const int GC_FORCE_THRESHOLD = 10000;

    /// <summary>
    /// Maximum number of attempts for retry policies
    /// </summary>
    public const int MAX_RETRY_ATTEMPTS = 3;

    /// <summary>
    /// Timeout in seconds for database commands
    /// </summary>
    public const int DB_COMMAND_TIMEOUT_SECONDS = 300;

    /// <summary>
    /// Default page size for paginated queries
    /// </summary>
    public const int DEFAULT_PAGE_SIZE = 25;

    /// <summary>
    /// Maximum page size for paginated queries
    /// </summary>
    public const int MAX_PAGE_SIZE = 1000;

    /// <summary>
    /// IRN format pattern for validation
    /// </summary>
    public const string IRN_FORMAT_PATTERN = @"^[A-Z0-9]+-[A-F0-9]{8}-\d{8}$";

    /// <summary>
    /// Expected IRN format example for error messages
    /// </summary>
    public const string IRN_FORMAT_EXAMPLE = "ITW00000001-E9E0C0D3-20240619";
}
