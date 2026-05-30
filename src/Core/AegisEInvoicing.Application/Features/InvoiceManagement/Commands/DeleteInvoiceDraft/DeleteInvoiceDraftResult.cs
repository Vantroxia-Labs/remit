using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.DeleteInvoiceDraft;

public record DeleteInvoiceDraftResult : GenericResult
{
    public new static DeleteInvoiceDraftResult Successful() => new()
    {
        IsSuccess = true,
        StatusCodes = HttpStatusCodes.OK.ToInt(),
        Message = "Draft deleted."
    };

    public new static DeleteInvoiceDraftResult NotFound(string message) => new()
    {
        IsSuccess = false,
        StatusCodes = HttpStatusCodes.NotFound.ToInt(),
        Message = message
    };

    public new static DeleteInvoiceDraftResult AuthorizationError(string? message = null) => new()
    {
        IsSuccess = false,
        StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
        Message = message ?? ResponseMessages.INSUFFICIENT_PERMISSIONS
    };
}
