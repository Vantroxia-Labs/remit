using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.Authentication.Commands.Logout;

public record LogoutResult : GenericResult
{
    public static new LogoutResult AuthorizationError(string? message = null)
    {
        return new LogoutResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message ?? ResponseMessages.INSUFFICIENT_PERMISSIONS
        };
    }

    public static new LogoutResult Successful()
    {
        return new LogoutResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.OPERATION_SUCCESSFUL
        };
    }

    public static new LogoutResult Failure(string? message = null)
    {
        return new LogoutResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.ExpectationFailed.ToInt(),
            Message = message ?? ResponseMessages.OPERATION_FAILED
        };
    }
}
