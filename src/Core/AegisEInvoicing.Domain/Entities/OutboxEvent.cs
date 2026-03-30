using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Entities;

public class OutboxEvent : Entity
{
    public string EventType { get; set; } = default!;
    public string EventData { get; set; } = default!;
    public DateTimeOffset OccurredOnUtc { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public OutboxEventStatus Status { get; set; }
    public bool IsProcessed => ProcessedOnUtc.HasValue;

    public static OutboxEvent Create(string eventType, string eventData)
    {
        var now = DateTimeOffset.Now;
        return new OutboxEvent
        {
            Id = Guid.CreateVersion7(),
            EventType = eventType,
            EventData = eventData,
            OccurredOnUtc = now,
            CreatedAt = now,
            RetryCount = 0,
            Status = OutboxEventStatus.Pending
        };
    }

    public void MarkAsProcessed()
    {
        ProcessedOnUtc = DateTimeOffset.Now;
        Status = OutboxEventStatus.Processed;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
        Status = OutboxEventStatus.Failed;
    }

    public void MarkAsProcessing()
    {
        Status = OutboxEventStatus.Processing;
    }
}