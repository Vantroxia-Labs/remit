using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.TransmitInvoice;

public record TransmitInvoiceResult : GenericResult
{
    public new static TransmitInvoiceResult Successful()
    {
        return new TransmitInvoiceResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.INVOICE_TRANSMISSION_SUCCESSFUL
        };
    }

    public new static TransmitInvoiceResult NotFound(string message)
    {
        return new TransmitInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }

    public new static TransmitInvoiceResult BadRequest(string message)
    {
        return new TransmitInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.BadRequest.ToInt(),
            Message = message
        };
    }

    public new static TransmitInvoiceResult Failure(string? message = null)
    {
        return new TransmitInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.ExpectationFailed.ToInt(),
            Message = message ?? ResponseMessages.INVOICE_TRANSMISSION_FAILED
        };
    }
}
