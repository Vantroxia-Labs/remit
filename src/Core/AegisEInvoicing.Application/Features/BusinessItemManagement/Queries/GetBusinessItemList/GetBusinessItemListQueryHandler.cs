using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Common.Security;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Queries.GetBusinessItemList;

public class GetBusinessItemListQueryHandler : IRequestHandler<GetBusinessItemListQuery, PaginatedList<BusinessItemSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<GetBusinessItemListQueryHandler> _logger;

    public GetBusinessItemListQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<GetBusinessItemListQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<PaginatedList<BusinessItemSummaryDto>> Handle(GetBusinessItemListQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
            {
                _logger.LogWarning("Business Not Found");
                throw new BadRequestException("Business Not Found");
            }

            var query = _context.BusinessItems
                .AsNoTracking()
                .Include(bi => bi.Business)
                .Include(bi => bi.ItemCategory)
                .AsQueryable();

            // Filter by business
            query = query.Where(bi => bi.BusinessID == _currentUser.BusinessId.Value);

            // Apply category filter if provided
            if (request.ItemCategoryId.HasValue)
            {
                query = query.Where(bi => bi.ItemCategoryId == request.ItemCategoryId.Value);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                // Sanitize search term to prevent SQL injection (VAPT finding)
                var searchTerm = InputSanitizationService.SanitizeSearchTerm(request.SearchTerm);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(bi =>
                        bi.Name.ToLower().Contains(searchTerm) ||
                        bi.ItemId.ToLower().Contains(searchTerm) ||
                        bi.ItemDescription.ToLower().Contains(searchTerm) ||
                        (bi.ServiceCode != null && bi.ServiceCode.Name.ToLower().Contains(searchTerm)) ||
                        (bi.ItemCategory != null && bi.ItemCategory.Name.ToLower().Contains(searchTerm)));
                }
            }

            // Apply sorting
            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending
                    ? query.OrderByDescending(bi => bi.Name)
                    : query.OrderBy(bi => bi.Name),
                "itemid" => request.SortDescending
                    ? query.OrderByDescending(bi => bi.ItemId)
                    : query.OrderBy(bi => bi.ItemId),
                "unitprice" => request.SortDescending
                    ? query.OrderByDescending(bi => bi.UnitPrice)
                    : query.OrderBy(bi => bi.UnitPrice),
                "category" => request.SortDescending
                    ? query.OrderByDescending(bi => bi.ItemCategory != null ? bi.ItemCategory.Name : "")
                    : query.OrderBy(bi => bi.ItemCategory != null ? bi.ItemCategory.Name : ""),
                "createdat" => request.SortDescending
                    ? query.OrderByDescending(bi => bi.CreatedAt)
                    : query.OrderBy(bi => bi.CreatedAt),
                _ => query.OrderBy(bi => bi.Name)
            };

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var businessItems = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(bi => new BusinessItemSummaryDto(
                    bi.Id,
                    bi.ItemId,
                    bi.Name,
                    bi.ServiceCode != null ? bi.ServiceCode.Code : "",
                    bi.ServiceCode != null ? bi.ServiceCode.Name : "",
                    bi.TaxCategory != null ? bi.TaxCategory.Name : "",
                    bi.ItemCategory != null ? bi.ItemCategory.Name : "",
                    bi.UnitPrice,
                    bi.Business != null ? bi.Business.Name : "",
                    bi.CreatedAt))
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Successfully retrieved {Count} business items (page {PageNumber} of {PageSize})", 
                businessItems.Count, request.PageNumber, request.PageSize);
            
            return new PaginatedList<BusinessItemSummaryDto>(businessItems, totalCount, request.PageNumber, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving business items: {Message}", ex.Message);
            return new PaginatedList<BusinessItemSummaryDto>(new List<BusinessItemSummaryDto>(), 0, request.PageNumber, request.PageSize);
        }
    }
}