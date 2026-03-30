using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitInvoice;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitBulkInvoice;

/// <summary>
/// Represents an error that occurred during bulk invoice processing
/// </summary>
public class BulkProcessingError
{
    /// <summary>
    /// Index of the invoice in the request list (0-based)
    /// </summary>
    public int InvoiceIndex { get; set; }

    /// <summary>
    /// Invoice number or identifier (if available)
    /// </summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// IRN of the invoice (if created)
    /// </summary>
    public string? IRN { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// The step where processing failed (create, validate, sign, transmit)
    /// </summary>
    public string? FailedAt { get; set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public string? ErrorDetails { get; set; }
}

/// <summary>
/// Result of the bulk create and submit invoice operation
/// </summary>
public class CreateAndSubmitBulkInvoiceResult
{
    /// <summary>
    /// Indicates if all invoices were successfully processed
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Total number of invoices processed
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Number of invoices that completed successfully (all steps succeeded)
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of invoices that failed at any step
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Overall result message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detailed results for each invoice
    /// </summary>
    public List<CreateAndSubmitInvoiceResult> Results { get; set; } = [];

    /// <summary>
    /// Summary of errors that occurred
    /// </summary>
    public List<BulkProcessingError> Errors { get; set; } = [];

    /// <summary>
    /// Total execution time for all invoices
    /// </summary>
    public TimeSpan? TotalExecutionTime { get; set; }

    /// <summary>
    /// HTTP status code for the overall operation
    /// </summary>
    public int StatusCodes { get; set; } = 200;

    /// <summary>
    /// Creates a successful bulk result
    /// </summary>
    public static CreateAndSubmitBulkInvoiceResult Successful(int totalProcessed)
    {
        return new CreateAndSubmitBulkInvoiceResult
        {
            Success = true,
            TotalProcessed = totalProcessed,
            SuccessCount = totalProcessed,
            FailedCount = 0,
            Message = $"All {totalProcessed} invoices created and submitted successfully",
            StatusCodes = 200
        };
    }

    /// <summary>
    /// Creates a partial success bulk result
    /// </summary>
    public static CreateAndSubmitBulkInvoiceResult PartialSuccess(
        int totalProcessed, 
        int successCount, 
        int failedCount)
    {
        return new CreateAndSubmitBulkInvoiceResult
        {
            Success = false,
            TotalProcessed = totalProcessed,
            SuccessCount = successCount,
            FailedCount = failedCount,
            Message = $"Bulk processing completed: {successCount} succeeded, {failedCount} failed out of {totalProcessed} total",
            StatusCodes = 207 // Multi-Status
        };
    }

    /// <summary>
    /// Creates a failed bulk result
    /// </summary>
    public static CreateAndSubmitBulkInvoiceResult Failed(string message, int statusCode = 400)
    {
        return new CreateAndSubmitBulkInvoiceResult
        {
            Success = false,
            Message = message,
            StatusCodes = statusCode
        };
    }
}
