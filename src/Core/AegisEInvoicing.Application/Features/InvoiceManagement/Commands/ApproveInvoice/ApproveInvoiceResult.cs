using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ApproveInvoice;

public record ApproveInvoiceResult : GenericResult
{
    public Guid? InvoiceId { get; init; }

    public static ApproveInvoiceResult Success(Guid invoiceId, string message = "Invoice approved successfully")
    {
        return new ApproveInvoiceResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = message,
            InvoiceId = invoiceId
        };
    }

    public new static ApproveInvoiceResult NotFound(string message = "Invoice not found")
    {
        return new ApproveInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }

    public new static ApproveInvoiceResult BadRequest(string message)
    {
        return new ApproveInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.BadRequest.ToInt(),
            Message = message
        };
    }

    public new static ApproveInvoiceResult AuthorizationError(string message = "Unauthorized")
    {
        return new ApproveInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message
        };
    }
}
