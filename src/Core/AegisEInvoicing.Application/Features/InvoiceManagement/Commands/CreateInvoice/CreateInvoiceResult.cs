using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoice;

public record CreateInvoiceResult : GenericResult
{
    public static CreateInvoiceResult Created(string? message = null)
    {
        return new CreateInvoiceResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.Created.ToInt(),
            Message = message ?? ResponseMessages.CREATED
        };
    }

    public new static CreateInvoiceResult NotFound(string message)
    {
        return new CreateInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }

    public new static CreateInvoiceResult BadRequest(string message)
    {
        return new CreateInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.BadRequest.ToInt(),
            Message = message
        };
    }

    public new static CreateInvoiceResult Failure(string? message = null)
    {
        return new CreateInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.ExpectationFailed.ToInt(),
            Message = message ?? ResponseMessages.OPERATION_FAILED
        };
    }

    public new static GenericResult AuthorizationError(string? message = null)
    {
        return new GenericResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message ?? ResponseMessages.INSUFFICIENT_PERMISSIONS
        };
    }
}
