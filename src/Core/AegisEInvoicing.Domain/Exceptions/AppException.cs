using System.Net;

namespace AegisEInvoicing.Domain.Exceptions;

/// <summary>
/// Base exception for app-specific errors
/// </summary>
public abstract class AppException : Exception
{
    public abstract HttpStatusCode StatusCode { get; }
    public virtual string ErrorCode { get; }
    public virtual object? Details { get; }
    protected AppException(string message, string? errorCode = null, object? details = null) : base(message)
    {
        ErrorCode = errorCode ?? GetType().Name.Replace("Exception", "");
        Details = details;
    }

    protected AppException(string message, Exception innerException, string? errorCode = null, object? details = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode ?? GetType().Name.Replace("Exception", "");
        Details = details;
    }
}