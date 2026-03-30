using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.RejectInvoice;

public record RejectInvoiceResult : GenericResult
{
    public Guid? InvoiceId { get; init; }

    public static RejectInvoiceResult Success(Guid invoiceId, string message = "Invoice rejected successfully")
    {
        return new RejectInvoiceResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = message,
            InvoiceId = invoiceId
        };
    }

    public new static RejectInvoiceResult NotFound(string message = "Invoice not found")
    {
        return new RejectInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }

    public new static RejectInvoiceResult BadRequest(string message)
    {
        return new RejectInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.BadRequest.ToInt(),
            Message = message
        };
    }

    public new static RejectInvoiceResult AuthorizationError(string message = "Unauthorized")
    {
        return new RejectInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message
        };
    }
}
