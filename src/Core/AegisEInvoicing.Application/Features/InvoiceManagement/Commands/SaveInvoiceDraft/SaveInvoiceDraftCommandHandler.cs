using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SaveInvoiceDraft;

public class SaveInvoiceDraftCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<SaveInvoiceDraftCommandHandler> logger)
    : IRequestHandler<SaveInvoiceDraftCommand, SaveInvoiceDraftResult>
{
    public async Task<SaveInvoiceDraftResult> Handle(SaveInvoiceDraftCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.BusinessId is null || currentUser.UserId is null)
            return SaveInvoiceDraftResult.AuthorizationError();

        // Update existing draft
        if (request.DraftId.HasValue)
        {
            var existing = await context.InvoiceDrafts
                .FirstOrDefaultAsync(d => d.Id == request.DraftId.Value
                    && d.BusinessId == currentUser.BusinessId.Value, cancellationToken);

            if (existing is null)
                return SaveInvoiceDraftResult.NotFound("Draft not found.");

            existing.Update(request.IssueDate, request.DraftPayload, request.PartyName);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Updated invoice draft {DraftId} for business {BusinessId}",
                existing.Id, currentUser.BusinessId);

            return SaveInvoiceDraftResult.Updated(existing.Id);
        }

        // Create new draft
        var draft = InvoiceDraft.Create(
            businessId: currentUser.BusinessId.Value,
            createdBy: currentUser.UserId.Value,
            issueDate: request.IssueDate,
            draftPayload: request.DraftPayload,
            partyName: request.PartyName);

        await context.InvoiceDrafts.AddAsync(draft, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created invoice draft {DraftId} for business {BusinessId}",
            draft.Id, currentUser.BusinessId);

        return SaveInvoiceDraftResult.Created(draft.Id);
    }
}
