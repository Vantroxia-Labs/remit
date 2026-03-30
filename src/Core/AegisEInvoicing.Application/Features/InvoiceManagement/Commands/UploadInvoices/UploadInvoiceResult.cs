using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UploadInvoices;

public record UploadInvoiceResult : GenericResult
{
    public int? TotalObjects { get; set; }
    public int? SuccessfulUploads { get; set; }
    public int? FailedUploads { get; set; }
    public Dictionary<string, string>? FailedUploadDetails { get; set; }

    public new static UploadInvoiceResult AuthorizationError(string? message = null)
    {
        return new UploadInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message ?? ResponseMessages.INSUFFICIENT_PERMISSIONS
        };
    }

    public new static UploadInvoiceResult Successful()
    {
        return new UploadInvoiceResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.OPERATION_SUCCESSFUL,
            FailedUploadDetails = [],
            TotalObjects = 0,
            SuccessfulUploads = 0,
            FailedUploads = 0
        };
    }

    public new static UploadInvoiceResult BadRequest(string message)
    {
        return new UploadInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.BadRequest.ToInt(),
            Message = message
        };
    }

    public new static UploadInvoiceResult NotFound(string message)
    {
        return new UploadInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }
}
