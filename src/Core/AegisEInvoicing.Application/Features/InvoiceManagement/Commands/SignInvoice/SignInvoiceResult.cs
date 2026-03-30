using AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetFirsApiConfiguration;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SignInvoice;

public record SignInvoiceResult : GenericResult
{
    public new static SignInvoiceResult Successful()
    {
        return new SignInvoiceResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.INVOICE_SIGNING_SUCCESSFUL
        };
    }

    public new static SignInvoiceResult NotFound(string message)
    {
        return new SignInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }

    public new static SignInvoiceResult BadRequest(string message)
    {
        return new SignInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.BadRequest.ToInt(),
            Message = message
        };
    }

    public new static SignInvoiceResult Failure(string message)
    {
        return new SignInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.ExpectationFailed.ToInt(),
            Message = message
        };
    }

    public new static SignInvoiceResult AuthorizationError(string? message = null)
    {
        return new SignInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message ?? ResponseMessages.INSUFFICIENT_PERMISSIONS
        };
    }
}
