using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.BusinessManagement;

namespace AegisEInvoicing.Domain.Entities;

/// <summary>
/// Tracks API usage for billing and rate limiting purposes
/// </summary>
public class ApiUsageTracking : AuditableEntity
{
    public Guid BusinessId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public string HttpMethod { get; private set; } = string.Empty;
    public int ResponseStatusCode { get; private set; }
    public long ResponseTimeMs { get; private set; }
    public long RequestSizeBytes { get; private set; }
    public long ResponseSizeBytes { get; private set; }
    public DateTimeOffset RequestTimestamp { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? ApiKeyUsed { get; private set; }
    public bool IsBillable { get; private set; }
    public decimal? Cost { get; private set; }
    
    public string? FIRSInvoiceId { get; private set; }
    public bool UsedAegisCredentials { get; private set; }

    // Navigation properties
    public Business Business { get; private set; } = null!;
    public UserManagement.User? User { get; private set; }

    private ApiUsageTracking() { } // EF Constructor

    public static ApiUsageTracking Create(
        Guid businessId,
        string endpoint,
        string httpMethod,
        DateTimeOffset requestTimestamp,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? apiKeyUsed = null)
    {
        return new ApiUsageTracking
        {
            Id = Guid.CreateVersion7(),
            BusinessId = businessId,
            UserId = userId,
            Endpoint = endpoint,
            HttpMethod = httpMethod,
            RequestTimestamp = requestTimestamp,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ApiKeyUsed = apiKeyUsed,
            IsBillable = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void RecordResponse(
        int statusCode,
        long responseTimeMs,
        long requestSizeBytes,
        long responseSizeBytes)
    {
        ResponseStatusCode = statusCode;
        ResponseTimeMs = responseTimeMs;
        RequestSizeBytes = requestSizeBytes;
        ResponseSizeBytes = responseSizeBytes;
    }

    public void RecordFIRSOperation(
        string? invoiceId,
        bool usedAegisCredentials)
    {
        FIRSInvoiceId = invoiceId;
        UsedAegisCredentials = usedAegisCredentials;

        // Calculate cost based on operation
        Cost = 0;
    }

    public void MarkAsNonBillable(string? reason = null)
    {
        IsBillable = false;
        Cost = 0;
    }
}

/// <summary>
/// Aggregated API usage for billing periods
/// </summary>
public class ApiUsageSummary : AuditableEntity
{
    public Guid BusinessId { get; private set; }
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public int TotalRequests { get; private set; }
    public int SuccessfulRequests { get; private set; }
    public int FailedRequests { get; private set; }
    public long TotalDataTransferredBytes { get; private set; }
    public decimal TotalCost { get; private set; }
    public Dictionary<string, int> EndpointUsage { get; private set; } = [];
    public Dictionary<string, decimal> EndpointCosts { get; private set; } = [];
    public int FIRSOperationsCount { get; private set; }
    public decimal FIRSOperationsCost { get; private set; }
    public bool IsFinalized { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }

    // Navigation properties
    public Business Business { get; private set; } = null!;

    private ApiUsageSummary() { } // EF Constructor

    public static ApiUsageSummary Create(
        Guid businessId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd)
    {
        return new ApiUsageSummary
        {
            Id = Guid.CreateVersion7(),
            BusinessId = businessId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateFromUsageRecords(IEnumerable<ApiUsageTracking> usageRecords)
    {
        if (IsFinalized)
            throw new InvalidOperationException("Cannot update finalized summary");

        TotalRequests = usageRecords.Count();
        SuccessfulRequests = usageRecords.Count(u => u.ResponseStatusCode >= 200 && u.ResponseStatusCode < 300);
        FailedRequests = TotalRequests - SuccessfulRequests;
        TotalDataTransferredBytes = usageRecords.Sum(u => u.RequestSizeBytes + u.ResponseSizeBytes);
        TotalCost = usageRecords.Where(u => u.IsBillable).Sum(u => u.Cost ?? 0);

        // Group by endpoint
        EndpointUsage = usageRecords
            .GroupBy(u => u.Endpoint)
            .ToDictionary(g => g.Key, g => g.Count());

        EndpointCosts = usageRecords
            .Where(u => u.IsBillable)
            .GroupBy(u => u.Endpoint)
            .ToDictionary(g => g.Key, g => g.Sum(u => u.Cost ?? 0));

        // FIRS specific
        FIRSOperationsCount = 0;
        FIRSOperationsCost = 0;
    }

    public void FinalizeSummary()
    {
        if (IsFinalized)
            throw new InvalidOperationException("Summary is already finalized");

        IsFinalized = true;
        FinalizedAt = DateTimeOffset.UtcNow;
    }
}