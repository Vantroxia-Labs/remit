using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Entities;

public class IntegrationLog : Entity
{
    public string Operation { get; set; } = default!;
    public string ExternalSystem { get; set; } = default!;
    public string RequestData { get; set; } = default!;
    public string? ResponseData { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? CorrelationId { get; set; }

    // Parameterless constructor for Entity Framework
    public IntegrationLog() 
    {
        Operation = string.Empty;
        ExternalSystem = string.Empty;  
        RequestData = string.Empty;
    }

    public static IntegrationLog Create(
        string operation,
        string externalSystem,
        string requestData,
        string? correlationId = null)
    {
        return new IntegrationLog
        {
            Id = Guid.CreateVersion7(),
            Operation = operation,
            ExternalSystem = externalSystem,
            RequestData = requestData,
            CorrelationId = correlationId,
            StartedAt = DateTime.UtcNow,
            IsSuccess = false
        };
    }

    public void MarkAsCompleted(string? responseData = null, bool isSuccess = true, string? errorMessage = null)
    {
        CompletedAt = DateTime.UtcNow;
        DurationMs = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
        ResponseData = responseData;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }
}