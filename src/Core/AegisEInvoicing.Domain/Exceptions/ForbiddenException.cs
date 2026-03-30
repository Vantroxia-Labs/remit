using System.Net;

namespace AegisEInvoicing.Domain.Exceptions;

/// <summary>
/// 403 Forbidden - User is authenticated but lacks permission
/// </summary>
public sealed class ForbiddenException : AppException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Forbidden;

    public ForbiddenException(string message = "Access denied", string? errorCode = null, object? details = null)
        : base(message, errorCode, details) { }

    public ForbiddenException(string message, Exception innerException, string? errorCode = null, object? details = null)
        : base(message, innerException, errorCode, details) { }
}