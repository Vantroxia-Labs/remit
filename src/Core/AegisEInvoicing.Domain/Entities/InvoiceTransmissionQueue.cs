using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Domain.Entities;

/// <summary>
/// Entity for queuing invoice transmission requests
/// </summary>
public class InvoiceTransmissionQueue : AuditableEntity
{
    /// <summary>
    /// Invoice Reference Number (IRN)
    /// </summary>
    public string Irn { get; private set; } = string.Empty;

    /// <summary>
    /// Transmission status to process
    /// </summary>
    public InvoiceStatus Status { get; private set; }

    /// <summary>
    /// Request payload as JSON
    /// </summary>
    public string RequestPayload { get; private set; } = string.Empty;

    /// <summary>
    /// Business context
    /// </summary>
    public Guid? BusinessId { get; private set; }

    /// <summary>
    /// User context
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Processing status
    /// </summary>
    public QueueStatus ProcessingStatus { get; private set; } = QueueStatus.Pending;

    /// <summary>
    /// Number of processing attempts
    /// </summary>
    public int AttemptCount { get; private set; } = 0;

    /// <summary>
    /// Last error message if processing failed
    /// </summary>
    public string? LastErrorMessage { get; private set; }

    /// <summary>
    /// When the request should be processed next (for retry logic)
    /// </summary>
    public DateTimeOffset? ProcessAfter { get; private set; }

    /// <summary>
    /// When the request was completed
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    private InvoiceTransmissionQueue() { } // EF Core

    public static InvoiceTransmissionQueue Create(
        string irn,
        InvoiceStatus status,
        string requestPayload,
        Guid? businessId = null,
        Guid? userId = null)
    {
        return new InvoiceTransmissionQueue
        {
            Id = Guid.CreateVersion7(),
            Irn = irn,
            Status = status,
            RequestPayload = requestPayload,
            BusinessId = businessId,
            UserId = userId,
            ProcessingStatus = QueueStatus.Pending,
            ProcessAfter = DateTimeOffset.UtcNow
        };
    }

    public void MarkAsProcessing()
    {
        ProcessingStatus = QueueStatus.Processing;
        AttemptCount++;
        LastErrorMessage = null;
    }

    public void MarkAsCompleted()
    {
        ProcessingStatus = QueueStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        LastErrorMessage = null;
    }

    public void MarkAsFailed(string errorMessage, int maxRetries = 3)
    {
        LastErrorMessage = errorMessage;

        if (AttemptCount >= maxRetries)
        {
            ProcessingStatus = QueueStatus.Failed;
        }
        else
        {
            ProcessingStatus = QueueStatus.Pending;
            // Exponential backoff: 1min, 5min, 15min
            var delayMinutes = Math.Pow(5, AttemptCount - 1);
            ProcessAfter = DateTimeOffset.UtcNow.AddMinutes(delayMinutes);
        }
    }
}

/// <summary>
/// Status of queue item processing
/// </summary>
public enum QueueStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}