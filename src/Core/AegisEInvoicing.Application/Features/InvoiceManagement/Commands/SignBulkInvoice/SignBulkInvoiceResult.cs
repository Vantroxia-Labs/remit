using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignBulkInvoice;

public record SignBulkInvoiceResult : GenericResult
{
    public int? TotalObjects { get; set; }
    public int? SuccessfullySigned { get; set; }
    public int? FailedSigning { get; set; }
    public Dictionary<string, string>? FailedSigningDetails { get; set; }

    public new static SignBulkInvoiceResult NotFound(string message)
    {
        return new SignBulkInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }

    public new static SignBulkInvoiceResult Successful()
    {
        return new SignBulkInvoiceResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.OPERATION_SUCCESSFUL,
            FailedSigningDetails = [],
            TotalObjects = 0,
            SuccessfullySigned = 0,
            FailedSigning = 0
        };
    }

    public new static SignBulkInvoiceResult Processing()
    {
        return new SignBulkInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Processing.ToInt(),
            Message = ResponseMessages.OPERATION_PROCESSING
        };
    }
}
