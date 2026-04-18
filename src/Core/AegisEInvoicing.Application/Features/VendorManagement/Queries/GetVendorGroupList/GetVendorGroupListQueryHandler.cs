using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorGroupList;

public class GetVendorGroupListQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GetVendorGroupListQueryHandler> logger) : IRequestHandler<GetVendorGroupListQuery, PaginatedList<VendorGroupSummaryDto>>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<GetVendorGroupListQueryHandler> _logger = logger;

    public async Task<PaginatedList<VendorGroupSummaryDto>> Handle(GetVendorGroupListQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new PaginatedList<VendorGroupSummaryDto>([], 0, request.PageNumber, request.PageSize);

            var query = _context.VendorGroups
                .AsNoTracking()
                .Where(g => g.BusinessId == _currentUser.BusinessId.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLowerInvariant();
                query = query.Where(g => g.Name.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(g => g.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(g => new VendorGroupSummaryDto(
                    g.Id,
                    g.Name,
                    g.Description,
                    g.Vendors.Count,
                    g.CreatedAt))
                .ToListAsync(cancellationToken);

            return new PaginatedList<VendorGroupSummaryDto>(items, totalCount, request.PageNumber, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vendor group list");
            return new PaginatedList<VendorGroupSummaryDto>([], 0, request.PageNumber, request.PageSize);
        }
    }
}
