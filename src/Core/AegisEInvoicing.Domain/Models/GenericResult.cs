using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;

namespace AegisEInvoicing.Domain.Models;

public record GenericResult
{
    public bool IsSuccess { get; set; } = false;
    public int StatusCodes { get; set; } = 200;
    public string Message { get; set; } = null!;

    public static GenericResult AuthorizationError(string? message = null)
    {
        return new GenericResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message ?? ResponseMessages.INSUFFICIENT_PERMISSIONS
        };
    }

    public static GenericResult Successful()
    {
        return new GenericResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.OPERATION_SUCCESSFUL
        };
    }

    public static GenericResult Created()
    {
        return new GenericResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.Created.ToInt(),
            Message = ResponseMessages.CREATED
        };
    }

    public static GenericResult Failure(string? message = null)
    {
        return new GenericResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.ExpectationFailed.ToInt(),
            Message = message ?? ResponseMessages.OPERATION_FAILED
        };
    }

    public static GenericResult Processing()
    {
        return new GenericResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Processing.ToInt(),
            Message = ResponseMessages.OPERATION_PROCESSING
        };
    }

    public static GenericResult NotFound(string message)
    {
        return new GenericResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }

    public static GenericResult BadRequest(string message)
    {
        return new GenericResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.BadRequest.ToInt(),
            Message = message
        };
    }

    public static GenericResult Updated(string message)
    {
        return new GenericResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.NoContent.ToInt(),
            Message = message
        };
    }

    public static GenericResult InternalServerError(string message)
    {
        return new GenericResult
        {
            IsSuccess = false,
            StatusCodes = 500,
            Message = message
        };
    }
}
