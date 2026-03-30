namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitInvoice;

/// <summary>
/// Represents the result of a single pipeline step execution
/// </summary>
public class PipelineStepResult
{
    /// <summary>
    /// Status of the step execution
    /// </summary>
    public string Status { get; set; } = "PENDING";  // PENDING | SUCCESS | FAILED | SKIPPED

    /// <summary>
    /// Message describing the step result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code from the step execution (if applicable)
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// When the step was executed
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional error details (if step failed)
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Creates a successful step result
    /// </summary>
    public static PipelineStepResult Success(string message, int? statusCode = null)
    {
        return new PipelineStepResult
        {
            Status = "SUCCESS",
            Message = message,
            StatusCode = statusCode,
            ExecutedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failed step result
    /// </summary>
    public static PipelineStepResult Failed(string message, int? statusCode = null, string? errorDetails = null)
    {
        return new PipelineStepResult
        {
            Status = "FAILED",
            Message = message,
            StatusCode = statusCode,
            ErrorDetails = errorDetails,
            ExecutedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a skipped step result
    /// </summary>
    public static PipelineStepResult Skipped(string message)
    {
        return new PipelineStepResult
        {
            Status = "SKIPPED",
            Message = message,
            ExecutedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Represents the execution status of the entire pipeline
/// </summary>
public class PipelineExecution
{
    /// <summary>
    /// Create invoice step result
    /// </summary>
    public PipelineStepResult? Create { get; set; }

    /// <summary>
    /// Validate invoice step result
    /// </summary>
    public PipelineStepResult? Validate { get; set; }

    /// <summary>
    /// Sign invoice step result
    /// </summary>
    public PipelineStepResult? Sign { get; set; }

    /// <summary>
    /// Transmit invoice step result
    /// </summary>
    public PipelineStepResult? Transmit { get; set; }

    /// <summary>
    /// Total execution time for the entire pipeline
    /// </summary>
    public TimeSpan? TotalExecutionTime { get; set; }

    /// <summary>
    /// Checks if all steps succeeded
    /// </summary>
    public bool AllStepsSucceeded()
    {
        return Create?.Status == "SUCCESS" &&
               Validate?.Status == "SUCCESS" &&
               Sign?.Status == "SUCCESS" &&
               Transmit?.Status == "SUCCESS";
    }

    /// <summary>
    /// Gets the first failed step name
    /// </summary>
    public string? GetFirstFailedStep()
    {
        if (Create?.Status == "FAILED") return "create";
        if (Validate?.Status == "FAILED") return "validate";
        if (Sign?.Status == "FAILED") return "sign";
        if (Transmit?.Status == "FAILED") return "transmit";
        return null;
    }
}
