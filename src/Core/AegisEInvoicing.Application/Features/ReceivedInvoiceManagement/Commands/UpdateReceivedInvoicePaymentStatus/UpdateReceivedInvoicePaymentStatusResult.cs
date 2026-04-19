using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Commands.UpdateReceivedInvoicePaymentStatus;

public record UpdateReceivedInvoicePaymentStatusResult : GenericResult
{
    public new static UpdateReceivedInvoicePaymentStatusResult NotFound(string message)
    {
        return new UpdateReceivedInvoicePaymentStatusResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }

    public new static UpdateReceivedInvoicePaymentStatusResult Updated(string message)
    {
        return new UpdateReceivedInvoicePaymentStatusResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = message
        };
    }

    public new static UpdateReceivedInvoicePaymentStatusResult AuthorizationError(string? message = null)
    {
        return new UpdateReceivedInvoicePaymentStatusResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message ?? ResponseMessages.INSUFFICIENT_PERMISSIONS
        };
    }

    public new static UpdateReceivedInvoicePaymentStatusResult BadRequest(string message)
    {
        return new UpdateReceivedInvoicePaymentStatusResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.BadRequest.ToInt(),
            Message = message
        };
    }

    public new static UpdateReceivedInvoicePaymentStatusResult Failure(string message)
    {
        return new UpdateReceivedInvoicePaymentStatusResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.ExpectationFailed.ToInt(),
            Message = message
        };
    }
}
