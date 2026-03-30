using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.PartyManagement.Queries.GetPartyList;

public class GetPartyListQueryHandler : IRequestHandler<GetPartyListQuery, PaginatedList<PartySummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<GetPartyListQueryHandler> _logger;

    public GetPartyListQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<GetPartyListQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<PaginatedList<PartySummaryDto>> Handle(GetPartyListQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Parties
                .AsNoTracking()
                .Include(p => p.Business)
                .AsQueryable();

            if (_currentUser.IsPlatformAdmin && request.BusinessId.HasValue)
                query = query.Where(p => p.BusinessID == request.BusinessId);

            if (!_currentUser.IsPlatformAdmin && _currentUser.BusinessId.HasValue)
                query = query.Where(p => p.BusinessID == _currentUser.BusinessId.Value);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.Email.ToLower().Contains(searchTerm) ||
                    p.TaxIdentificationNumber.Value.Contains(searchTerm) ||
                    (p.Business != null && p.Business.Name.ToLower().Contains(searchTerm)));
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending
                    ? query.OrderByDescending(p => p.Name)
                    : query.OrderBy(p => p.Name),
                "email" => request.SortDescending
                    ? query.OrderByDescending(p => p.Email)
                    : query.OrderBy(p => p.Email),
                "createdat" => request.SortDescending
                    ? query.OrderByDescending(p => p.CreatedAt)
                    : query.OrderBy(p => p.CreatedAt),
                "businessname" => request.SortDescending
                    ? query.OrderByDescending(p => p.Business != null ? p.Business.Name : "")
                    : query.OrderBy(p => p.Business != null ? p.Business.Name : ""),
                _ => query.OrderBy(p => p.Name)
            };

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var parties = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new PartySummaryDto(
                    p.Id,
                    p.Name,
                    p.Email,
                    p.Phone,
                    p.TaxIdentificationNumber.Value,
                    p.CreatedAt))
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Successfully retrieved {Count} parties (page {PageNumber} of {PageSize})", parties.Count, request.PageNumber, request.PageSize);
            return new PaginatedList<PartySummaryDto>(parties, totalCount, request.PageNumber, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving parties: {Message}", ex.Message);
            return new PaginatedList<PartySummaryDto>(new List<PartySummaryDto>(), 0, request.PageNumber, request.PageSize);
        }
    }
}