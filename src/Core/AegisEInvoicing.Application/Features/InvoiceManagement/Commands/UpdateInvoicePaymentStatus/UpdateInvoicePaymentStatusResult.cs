using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignInvoice;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoicePaymentStatus;

public record UpdateInvoicePaymentStatusResult : GenericResult
{
    public new static UpdateInvoicePaymentStatusResult Successful()
    {
        return new UpdateInvoicePaymentStatusResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.INVOICE_UPDATED_SUCCESS
        };
    }

    public new static UpdateInvoicePaymentStatusResult NotFound(string message)
    {
        return new UpdateInvoicePaymentStatusResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }

    public new static UpdateInvoicePaymentStatusResult Updated(string message)
    {
        return new UpdateInvoicePaymentStatusResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = message
        };
    }

    public new static UpdateInvoicePaymentStatusResult AuthorizationError(string? message = null)
    {
        return new UpdateInvoicePaymentStatusResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message ?? ResponseMessages.INSUFFICIENT_PERMISSIONS
        };
    }

    public new static UpdateInvoicePaymentStatusResult BadRequest(string message)
    {
        return new UpdateInvoicePaymentStatusResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.BadRequest.ToInt(),
            Message = message
        };
    }

    public new static UpdateInvoicePaymentStatusResult Failure(string message)
    {
        return new UpdateInvoicePaymentStatusResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.ExpectationFailed.ToInt(),
            Message = message
        };
    }
}