using System.Net;

namespace AegisEInvoicing.Domain.Exceptions;

/// <summary>
/// 400 Bad Request - Invalid input or request format
/// </summary>
public sealed class BadRequestException : AppException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;

    public BadRequestException(string message, string? errorCode = null, object? details = null)
        : base(message, errorCode, details) { }

    public BadRequestException(string message, Exception innerException, string? errorCode = null, object? details = null)
        : base(message, innerException, errorCode, details) { }
}