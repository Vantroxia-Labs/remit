using System.Net;

namespace AegisEInvoicing.Domain.Exceptions;

/// <summary>
/// 409 Conflict - Resource already exists or conflict with current state
/// </summary>
public sealed class ConflictException : AppException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;

    public ConflictException(string message, string? errorCode = null, object? details = null)
        : base(message, errorCode, details) { }

    public ConflictException(string message, Exception innerException, string? errorCode = null, object? details = null)
        : base(message, innerException, errorCode, details) { }
}