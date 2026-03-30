using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.ItemCategoryManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.ItemCategoryManagement.Queries.GetItemCategoryById;

public class GetItemCategoryByIdQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GetItemCategoryByIdQueryHandler> logger) : IRequestHandler<GetItemCategoryByIdQuery, GetItemCategoryByIdResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<GetItemCategoryByIdQueryHandler> _logger = logger;
    public async Task<GetItemCategoryByIdResult> Handle(GetItemCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.ItemCategories
               .AsNoTracking()
               .Where(i => i.Id == request.Id);

            if (!_currentUser.IsPlatformAdmin && _currentUser.BusinessId.HasValue)
            {
                query = query.Where(i => i.BusinessID == _currentUser.BusinessId.Value);
            }

            var itemCategory = await query.FirstOrDefaultAsync(cancellationToken);

            if (itemCategory is null)
            {
                return new GetItemCategoryByIdResult
                {
                    Success = false,
                    Message = "Item Category not found"
                };
            }

            var itemCategoryDto = new ItemCategoryDto(
                itemCategory.Id,
                itemCategory.Name,
                itemCategory.Description,
                itemCategory.CreatedAt);

            return new GetItemCategoryByIdResult
            {
                Success = true,
                Message = "Item Category Successfully returned",
                ItemCategory = itemCategoryDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new GetItemCategoryByIdResult
            {
                Success = false,
                Message = "Item Category cannot be returned at this time. Please try again later."
            };
        }
    }
}
