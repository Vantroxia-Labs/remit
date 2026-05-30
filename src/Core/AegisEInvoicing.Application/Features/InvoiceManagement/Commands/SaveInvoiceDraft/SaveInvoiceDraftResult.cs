using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SaveInvoiceDraft;

public record SaveInvoiceDraftResult : GenericResult
{
    public Guid? DraftId { get; init; }

    public static SaveInvoiceDraftResult Created(Guid draftId) => new()
    {
        IsSuccess = true,
        StatusCodes = HttpStatusCodes.Created.ToInt(),
        Message = "Draft saved.",
        DraftId = draftId
    };

    public static SaveInvoiceDraftResult Updated(Guid draftId) => new()
    {
        IsSuccess = true,
        StatusCodes = HttpStatusCodes.OK.ToInt(),
        Message = "Draft updated.",
        DraftId = draftId
    };

    public new static SaveInvoiceDraftResult NotFound(string message) => new()
    {
        IsSuccess = false,
        StatusCodes = HttpStatusCodes.NotFound.ToInt(),
        Message = message
    };

    public new static SaveInvoiceDraftResult AuthorizationError(string? message = null) => new()
    {
        IsSuccess = false,
        StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
        Message = message ?? ResponseMessages.INSUFFICIENT_PERMISSIONS
    };
}
