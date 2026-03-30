using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignBulkInvoice;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.TransmitBulkInvoice;

public record TransmitBulkInvoiceResult : GenericResult
{
    public int? TotalObjects { get; set; }
    public int? SuccessfullyTransmitted { get; set; }
    public int? FailedTransmission { get; set; }
    public Dictionary<string, string>? FailedTransmissionDetails { get; set; }
    public new static TransmitBulkInvoiceResult Successful()
    {
        return new TransmitBulkInvoiceResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.INVOICE_TRANSMISSION_SUCCESSFUL,
            FailedTransmissionDetails = [],
            TotalObjects = 0,
            SuccessfullyTransmitted = 0,
            FailedTransmission = 0
        };
    }

    public new static TransmitBulkInvoiceResult NotFound(string message)
    {
        return new TransmitBulkInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }

    public new static TransmitBulkInvoiceResult Processing()
    {
        return new TransmitBulkInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Processing.ToInt(),
            Message = ResponseMessages.OPERATION_PROCESSING
        };
    }

    public new static TransmitBulkInvoiceResult Failure(string? message = null)
    {
        return new TransmitBulkInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.ExpectationFailed.ToInt(),
            Message = message ?? ResponseMessages.INVOICE_TRANSMISSION_FAILED
        };
    }
}