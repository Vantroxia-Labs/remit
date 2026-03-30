using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;

public record ValidateInvoiceResult : GenericResult
{

    public new static ValidateInvoiceResult AuthorizationError(string? message = null)
    {
        return new ValidateInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message ?? ResponseMessages.INSUFFICIENT_PERMISSIONS
        };
    }
    public new static ValidateInvoiceResult Successful()
    {
        return new ValidateInvoiceResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.INVOICE_VALIDATION_SUCCESSFUL
        };
    }    

    public new static ValidateInvoiceResult NotFound(string message)
    {
        return new ValidateInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }


    public new static ValidateInvoiceResult BadRequest(string message)
    {
        return new ValidateInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.BadRequest.ToInt(),
            Message = message
        };
    }

    public new static ValidateInvoiceResult Failure(string? message = null)
    {
        return new ValidateInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.ExpectationFailed.ToInt(),
            Message = message ?? ResponseMessages.OPERATION_FAILED
        };
    }
}