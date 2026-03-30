using System.Net;

namespace AegisEInvoicing.Domain.Exceptions;

/// <summary>
/// 401 Unauthorized - Authentication required or invalid
/// </summary>
public sealed class AuthenticationException : AppException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;

    public AuthenticationException(string message = "Authentication required", string? errorCode = null, object? details = null)
        : base(message, errorCode, details) { }

    public AuthenticationException(string message, Exception innerException, string? errorCode = null, object? details = null)
        : base(message, innerException, errorCode, details) { }
}
