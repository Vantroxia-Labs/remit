using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceIrns;

public record GetInvoiceIrnsResult : GenericResult
{
    public List<InvoiceIrnData>? Irns { get; init; } = [];

    public new static GetInvoiceIrnsResult AuthorizationError(string? message = null)
    {
        return new GetInvoiceIrnsResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message ?? ResponseMessages.INSUFFICIENT_PERMISSIONS
        };
    }

    public static GetInvoiceIrnsResult Successful(List<InvoiceIrnData>? irns)
    {
        return new GetInvoiceIrnsResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Irns = irns,
            Message = ResponseMessages.OPERATION_SUCCESSFUL
        };
    }

    public new static GetInvoiceIrnsResult NotFound(string message)
    {
        return new GetInvoiceIrnsResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }
}

public record InvoiceIrnData
{
    public string Irn { get; init; } = string.Empty;
    public DateOnly IssueDate { get; init; }
}
