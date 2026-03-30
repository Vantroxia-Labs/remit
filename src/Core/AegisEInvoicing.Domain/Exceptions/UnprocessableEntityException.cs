using System.Net;

namespace AegisEInvoicing.Domain.Exceptions;

/// <summary>
/// 422 Unprocessable Entity - Valid request format but semantic errors
/// </summary>
public sealed class UnprocessableEntityException : AppException
{
    public override HttpStatusCode StatusCode => HttpStatusCode.UnprocessableEntity;

    public UnprocessableEntityException(string message, string? errorCode = null, object? details = null)
        : base(message, errorCode, details) { }

    public UnprocessableEntityException(string message, Exception innerException, string? errorCode = null, object? details = null)
        : base(message, innerException, errorCode, details) { }
}