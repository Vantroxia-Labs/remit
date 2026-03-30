using System.Net;

namespace AegisEInvoicing.Infrastructure.Services;

/// <summary>
/// Exception specific to integration operations
/// </summary>
public sealed class IntegrationException : Exception
{
    /// <summary>
    /// HTTP status code associated with the error
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Correlation ID for tracking the request
    /// </summary>
    public string CorrelationId { get; }

    /// <summary>
    /// Whether the operation is retryable
    /// </summary>
    public bool IsRetryable { get; }

    /// <summary>
    /// Additional context data
    /// </summary>
    public Dictionary<string, object> Context { get; }

    public IntegrationException(
        string message, 
        HttpStatusCode statusCode, 
        string correlationId, 
        Exception? innerException = null) 
        : base(message, innerException)
    {
        StatusCode = statusCode;
        CorrelationId = correlationId;
        IsRetryable = DetermineIfRetryable(statusCode);
        Context = [];
    }

    public IntegrationException(
        string message, 
        HttpStatusCode statusCode, 
        string correlationId,
        bool isRetryable,
        Exception? innerException = null) 
        : base(message, innerException)
    {
        StatusCode = statusCode;
        CorrelationId = correlationId;
        IsRetryable = isRetryable;
        Context = [];
    }

    /// <summary>
    /// Add context information to the exception
    /// </summary>
    public IntegrationException WithContext(string key, object value)
    {
        Context[key] = value;
        return this;
    }

    /// <summary>
    /// Determine if an HTTP status code indicates a retryable error
    /// </summary>
    private static bool DetermineIfRetryable(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            // 5xx server errors are generally retryable
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            HttpStatusCode.HttpVersionNotSupported => true,
            HttpStatusCode.InsufficientStorage => true,
            
            // Some 4xx errors that might be temporary
            HttpStatusCode.RequestTimeout => true,
            HttpStatusCode.TooManyRequests => true,
            
            // Most 4xx errors are not retryable (client errors)
            HttpStatusCode.BadRequest => false,
            HttpStatusCode.Unauthorized => false,
            HttpStatusCode.Forbidden => false,
            HttpStatusCode.NotFound => false,
            HttpStatusCode.MethodNotAllowed => false,
            HttpStatusCode.Conflict => false,
            HttpStatusCode.Gone => false,
            HttpStatusCode.UnprocessableEntity => false,
            
            // Default to not retryable for unknown status codes
            _ => false
        };
    }

    public override string ToString()
    {
        var contextInfo = Context.Count != 0
            ? $"\nContext: {string.Join(", ", Context.Select(kvp => $"{kvp.Key}={kvp.Value}"))}"
            : string.Empty;

        return $"{base.ToString()}\n" +
               $"StatusCode: {StatusCode} ({(int)StatusCode})\n" +
               $"CorrelationId: {CorrelationId}\n" +
               $"IsRetryable: {IsRetryable}" +
               contextInfo;
    }
}