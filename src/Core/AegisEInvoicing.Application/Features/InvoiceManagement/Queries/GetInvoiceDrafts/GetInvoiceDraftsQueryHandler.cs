using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceDrafts;

public class GetInvoiceDraftsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<GetInvoiceDraftsQuery, List<InvoiceDraftDto>>
{
    public async Task<List<InvoiceDraftDto>> Handle(GetInvoiceDraftsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.BusinessId is null)
            return [];

        return await context.InvoiceDrafts
            .Where(d => d.BusinessId == currentUser.BusinessId.Value)
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .Select(d => new InvoiceDraftDto
            {
                Id = d.Id,
                PartyName = d.PartyName,
                IssueDate = d.IssueDate,
                DraftPayload = d.DraftPayload,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
