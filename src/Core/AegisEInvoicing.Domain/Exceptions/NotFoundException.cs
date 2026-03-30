using System.Net;

namespace AegisEInvoicing.Domain.Exceptions;

/// <summary>
/// 404 Not Found - Resource doesn't exist
/// </summary>
public sealed class NotFoundException : AppException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;

    public NotFoundException(string message, string? errorCode = null, object? details = null)
        : base(message, errorCode, details) { }

    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with identifier '{key}' was not found", "ResourceNotFound", new { ResourceName = resourceName, Key = key }) { }

    public NotFoundException(string message, Exception innerException, string? errorCode = null, object? details = null)
        : base(message, innerException, errorCode, details) { }
}