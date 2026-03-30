using System.Net;

namespace AegisEInvoicing.Domain.Exceptions;

/// <summary>
/// 429 Too Many Requests - Rate limiting
/// </summary>
public sealed class TooManyRequestsException : AppException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.TooManyRequests;

    public TooManyRequestsException(string message = "Too many requests", string? errorCode = null, object? details = null)
        : base(message, errorCode, details) { }

    public TooManyRequestsException(string message, Exception innerException, string? errorCode = null, object? details = null)
        : base(message, innerException, errorCode, details) { }
}
