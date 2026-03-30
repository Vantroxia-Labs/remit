using System.Net;

namespace AegisEInvoicing.Domain.Exceptions;

/// <summary>
/// 503 Service Unavailable - External service down or maintenance
/// </summary>
public sealed class ServiceUnavailableException : AppException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.ServiceUnavailable;

    public ServiceUnavailableException(string message = "Service temporarily unavailable", string? errorCode = null, object? details = null)
        : base(message, errorCode, details) { }

    public ServiceUnavailableException(string message, Exception innerException, string? errorCode = null, object? details = null)
        : base(message, innerException, errorCode, details) { }
}
