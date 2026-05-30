using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorList;

public class GetVendorListQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GetVendorListQueryHandler> logger) : IRequestHandler<GetVendorListQuery, PaginatedList<VendorSummaryDto>>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<GetVendorListQueryHandler> _logger = logger;

    public async Task<PaginatedList<VendorSummaryDto>> Handle(GetVendorListQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new PaginatedList<VendorSummaryDto>([], 0, request.PageNumber, request.PageSize);

            var query = _context.Vendors
                .AsNoTracking()
                .Include(v => v.VendorGroup)
                .Where(v => v.BusinessId == _currentUser.BusinessId.Value);

            if (request.VendorGroupId.HasValue)
                query = query.Where(v => v.VendorGroupId == request.VendorGroupId.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLowerInvariant();
                query = query.Where(v => v.BusinessName.ToLower().Contains(term) || v.Email.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(v => v.BusinessName)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(v => new VendorSummaryDto(
                    v.Id,
                    v.BusinessName,
                    v.Email,
                    v.Phone,
                    v.Status,
                    v.VendorGroupId,
                    v.VendorGroup.Name,
                    v.CreatedAt))
                .ToListAsync(cancellationToken);

            return new PaginatedList<VendorSummaryDto>(items, totalCount, request.PageNumber, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vendor list");
            return new PaginatedList<VendorSummaryDto>([], 0, request.PageNumber, request.PageSize);
        }
    }
}
