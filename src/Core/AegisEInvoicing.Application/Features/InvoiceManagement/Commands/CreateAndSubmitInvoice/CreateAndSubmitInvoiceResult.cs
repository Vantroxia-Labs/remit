using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitInvoice;

/// <summary>
/// Result of the consolidated create and submit invoice operation
/// </summary>
public class CreateAndSubmitInvoiceResult
{
    /// <summary>
    /// Indicates if the entire pipeline succeeded (all steps succeeded)
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The created invoice ID (null if creation failed)
    /// </summary>
    public Guid? InvoiceId { get; set; }

    /// <summary>
    /// The invoice reference number (IRN)
    /// </summary>
    public string? IRN { get; set; }

    /// <summary>
    /// Current status of the invoice (reflects the last successful step)
    /// </summary>
    public InvoiceStatus CurrentStatus { get; set; }

    /// <summary>
    /// Overall result message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detailed pipeline execution results for each step
    /// </summary>
    public PipelineExecution Pipeline { get; set; } = new();

    /// <summary>
    /// The name of the step where the pipeline first failed (null if all succeeded)
    /// </summary>
    public string? FailedAt { get; set; }

    /// <summary>
    /// Additional error details for debugging
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// HTTP status code for the overall operation
    /// </summary>
    public int StatusCodes { get; set; } = 200;

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static CreateAndSubmitInvoiceResult Successful(Guid invoiceId, string irn)
    {
        return new CreateAndSubmitInvoiceResult
        {
            Success = true,
            InvoiceId = invoiceId,
            IRN = irn,
            CurrentStatus = InvoiceStatus.TRANSMITTED,
            Message = "Invoice created and submitted successfully through the entire pipeline",
            StatusCodes = 200
        };
    }

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static CreateAndSubmitInvoiceResult Failed(string message, int statusCode = 400, string? errorDetails = null)
    {
        return new CreateAndSubmitInvoiceResult
        {
            Success = false,
            Message = message,
            ErrorDetails = errorDetails,
            StatusCodes = statusCode
        };
    }

    /// <summary>
    /// Creates a partial success result (some steps failed)
    /// </summary>
    public static CreateAndSubmitInvoiceResult PartialSuccess(
        Guid invoiceId, 
        string irn, 
        InvoiceStatus currentStatus, 
        string failedAt,
        string message)
    {
        return new CreateAndSubmitInvoiceResult
        {
            Success = false,
            InvoiceId = invoiceId,
            IRN = irn,
            CurrentStatus = currentStatus,
            FailedAt = failedAt,
            Message = message,
            StatusCodes = 207 // Multi-Status
        };
    }
}
