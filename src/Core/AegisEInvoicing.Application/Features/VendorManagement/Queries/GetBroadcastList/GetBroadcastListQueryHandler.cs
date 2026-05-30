using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetBroadcastList;

public class GetBroadcastListQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GetBroadcastListQueryHandler> logger) : IRequestHandler<GetBroadcastListQuery, PaginatedList<InvoiceBroadcastSummaryDto>>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<GetBroadcastListQueryHandler> _logger = logger;

    public async Task<PaginatedList<InvoiceBroadcastSummaryDto>> Handle(GetBroadcastListQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new PaginatedList<InvoiceBroadcastSummaryDto>([], 0, request.PageNumber, request.PageSize);

            var query = _context.InvoiceBroadcasts
                .AsNoTracking()
                .Where(b => b.BusinessId == _currentUser.BusinessId.Value);

            if (request.Status.HasValue)
                query = query.Where(b => b.Status == request.Status.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(b => new InvoiceBroadcastSummaryDto(
                    b.Id,
                    b.Title,
                    b.InvoiceTypeCode,
                    b.DueDate,
                    b.RequiresApproval,
                    b.IsApprovalLocked,
                    b.Status,
                    b.Currency,
                    b.Note,
                    b.BroadcastVendors.Count,
                    b.BroadcastVendors.Count(bv => bv.InvoiceId != null),
                    b.CreatedAt))
                .ToListAsync(cancellationToken);

            return new PaginatedList<InvoiceBroadcastSummaryDto>(items, totalCount, request.PageNumber, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving broadcast list");
            return new PaginatedList<InvoiceBroadcastSummaryDto>([], 0, request.PageNumber, request.PageSize);
        }
    }
}
