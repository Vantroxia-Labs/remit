using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.PartyManagement.Queries.GetPartiesByBusinessId;

public class GetPartiesByBusinessIdQueryHandler : IRequestHandler<GetPartiesByBusinessIdQuery, PaginatedList<PartySummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<GetPartiesByBusinessIdQueryHandler> _logger;

    public GetPartiesByBusinessIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<GetPartiesByBusinessIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<PaginatedList<PartySummaryDto>> Handle(GetPartiesByBusinessIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
            {
                _logger.LogWarning("Business Not Found");
                throw new NotFoundException("Business Not Found");
            }

            var query = _context.Parties
                .AsNoTracking()
                .Where(p => p.BusinessID == _currentUser.BusinessId.Value);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.Email.ToLower().Contains(searchTerm) ||
                    p.TaxIdentificationNumber.Value.Contains(searchTerm));
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

            _logger.LogDebug("Successfully retrieved {Count} parties for business {BusinessId} (page {PageNumber} of {PageSize})", parties.Count, _currentUser.BusinessId.Value, request.PageNumber, request.PageSize);
            return new PaginatedList<PartySummaryDto>(parties, totalCount, request.PageNumber, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving parties for business: {Message}", ex.Message);
            return new PaginatedList<PartySummaryDto>(new List<PartySummaryDto>(), 0, request.PageNumber, request.PageSize);
        }
    }
}