using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateBulkInvoice;

public record ValidateBulkInvoiceResult : GenericResult
{
    public int? TotalObjects { get; set; }
    public int? SuccessfullyValidated { get; set; }
    public int? FailedValidation { get; set; }
    public Dictionary<string, string>? FailedValidationDetails { get; set; }

    public new static ValidateBulkInvoiceResult AuthorizationError(string? message = null)
    {
        return new ValidateBulkInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message ?? ResponseMessages.INSUFFICIENT_PERMISSIONS
        };
    }

    public new static ValidateBulkInvoiceResult Successful()
    {
        return new ValidateBulkInvoiceResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.OPERATION_SUCCESSFUL,
            FailedValidationDetails = [],
            TotalObjects = 0,
            SuccessfullyValidated = 0,
            FailedValidation = 0
        };
    }

    public new static ValidateBulkInvoiceResult NotFound(string message)
    {
        return new ValidateBulkInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }

    public new static ValidateBulkInvoiceResult Processing()
    {
        return new ValidateBulkInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Processing.ToInt(),
            Message = ResponseMessages.OPERATION_PROCESSING
        };
    }
}
