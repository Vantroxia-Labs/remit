using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.ItemCategoryManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.ItemCategoryManagement.Queries.GetItemCategoryList;

public class GetItemCategoryListQueryHandler : IRequestHandler<GetItemCategoryListQuery, PaginatedList<ItemCategorySummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<GetItemCategoryListQueryHandler> _logger;

    public GetItemCategoryListQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<GetItemCategoryListQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<PaginatedList<ItemCategorySummaryDto>> Handle(GetItemCategoryListQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
            {
                _logger.LogWarning("Business Not Found");
                throw new BadRequestException("Business Not Found");
            }

            var query = _context.ItemCategories
                .AsNoTracking()
                .Include(ic => ic.Business)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(ic =>
                    ic.Name.ToLower().Contains(searchTerm) ||
                    ic.Description.ToLower().Contains(searchTerm) ||
                    (ic.Business != null && ic.Business.Name.ToLower().Contains(searchTerm)));
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending
                    ? query.OrderByDescending(ic => ic.Name)
                    : query.OrderBy(ic => ic.Name),
                "description" => request.SortDescending
                    ? query.OrderByDescending(ic => ic.Description)
                    : query.OrderBy(ic => ic.Description),
                "createdat" => request.SortDescending
                    ? query.OrderByDescending(ic => ic.CreatedAt)
                    : query.OrderBy(ic => ic.CreatedAt),
                "businessname" => request.SortDescending
                    ? query.OrderByDescending(ic => ic.Business != null ? ic.Business.Name : "")
                    : query.OrderBy(ic => ic.Business != null ? ic.Business.Name : ""),
                _ => query.OrderBy(ic => ic.Name)
            };

            query = query.Where(ic => ic.BusinessID == _currentUser.BusinessId.Value);

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var itemCategories = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(ic => new ItemCategorySummaryDto(
                    ic.Id,
                    ic.Name,
                    ic.Description,
                    ic.CreatedAt))
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Successfully retrieved {Count} item categories (page {PageNumber} of {PageSize})", itemCategories.Count, request.PageNumber, request.PageSize);
            return new PaginatedList<ItemCategorySummaryDto>(itemCategories, totalCount, request.PageNumber, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving item categories: {Message}", ex.Message);
            return new PaginatedList<ItemCategorySummaryDto>(new List<ItemCategorySummaryDto>(), 0, request.PageNumber, request.PageSize);
        }
    }
}