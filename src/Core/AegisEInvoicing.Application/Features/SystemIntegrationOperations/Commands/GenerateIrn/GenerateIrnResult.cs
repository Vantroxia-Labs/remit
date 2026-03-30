using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.SystemIntegrationOperations.Commands.GenerateIrn;

public record GenerateIrnResult : GenericResult
{
    public string Irn { get; private set; } = string.Empty;

    public new static GenerateIrnResult NotFound(string message)
    {
        return new GenerateIrnResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }

    public new static GenerateIrnResult AuthorizationError(string? message = null)
    {
        return new GenerateIrnResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message ?? ResponseMessages.INSUFFICIENT_PERMISSIONS
        };
    }

    public static GenerateIrnResult Successful(string irn)
    {
        return new GenerateIrnResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.GENERATE_IRN_SUCCESS,
            Irn = irn
        };
    }

    public new static GenerateIrnResult Failure(string? message = null)
    {
        return new GenerateIrnResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.ExpectationFailed.ToInt(),
            Message = message ?? ResponseMessages.GENERATE_IRN_FAILED
        };
    }
}
