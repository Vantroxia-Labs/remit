using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.DeleteInvoiceDraft;

public class DeleteInvoiceDraftCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<DeleteInvoiceDraftCommandHandler> logger)
    : IRequestHandler<DeleteInvoiceDraftCommand, DeleteInvoiceDraftResult>
{
    public async Task<DeleteInvoiceDraftResult> Handle(DeleteInvoiceDraftCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.BusinessId is null)
            return DeleteInvoiceDraftResult.AuthorizationError();

        var draft = await context.InvoiceDrafts
            .FirstOrDefaultAsync(d => d.Id == request.DraftId
                && d.BusinessId == currentUser.BusinessId.Value, cancellationToken);

        if (draft is null)
            return DeleteInvoiceDraftResult.NotFound("Draft not found.");

        draft.IsDeleted = true;
        draft.DeletedAt = DateTimeOffset.UtcNow;
        draft.DeletedBy = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted invoice draft {DraftId} for business {BusinessId}",
            request.DraftId, currentUser.BusinessId);

        return DeleteInvoiceDraftResult.Successful();
    }
}
