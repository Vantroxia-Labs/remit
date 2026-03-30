using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Queries.GetBusinessItemById;

public class GetBusinessItemByIdQueryHandler : IRequestHandler<GetBusinessItemByIdQuery, BusinessItemByIdResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<GetBusinessItemByIdQueryHandler> _logger;

    public GetBusinessItemByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<GetBusinessItemByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<BusinessItemByIdResult> Handle(GetBusinessItemByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.UserId.HasValue)
            {
                _logger.LogWarning("Unauthorized attempt to delete business item {BusinessItemId}", request.Id);
                throw new AuthenticationException("User authentication required");
            }

            if (!_currentUser.BusinessId.HasValue)
            {
                _logger.LogWarning("Business not found");
                throw new ForbiddenException("Business not found");
            }

            var businessItem = await _context.BusinessItems
                .AsNoTracking()
                .Include(bi => bi.Business)
                .Include(bi => bi.ItemCategory)
                .FirstOrDefaultAsync(bi => bi.Id == request.Id 
                && bi.BusinessID == _currentUser.BusinessId.Value,
                    cancellationToken);

            if (businessItem is null)
            {
                _logger.LogWarning("Attempt to get non-existent business item {BusinessItemId}", request.Id);
                throw new NotFoundException("Business item not found");
            }

            var businessItemDto = new BusinessItemDto(
                businessItem.Id,
                businessItem.ItemId,
                businessItem.Name,
                new ServiceCodeDto(businessItem.ServiceCode.Code, businessItem.ServiceCode.Name),
                new TaxCategoryDto(businessItem.TaxCategory.Name, businessItem.TaxCategory.Percent),
                businessItem.ItemCategoryId,
                businessItem.ItemCategory?.Name,
                businessItem.ItemDescription,
                businessItem.UnitPrice,
                businessItem.BusinessID,
                businessItem.Business?.Name,
                businessItem.CreatedAt,
                businessItem.UpdatedAt,
                businessItem.CreatedBy,
                businessItem.UpdatedBy);

            _logger.LogDebug("Successfully retrieved business item {BusinessItemId}", request.Id);
            return new BusinessItemByIdResult(true, 
                "Successfully retrieved business item",
                businessItemDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving business item {BusinessItemId}: {Message}", request.Id, ex.Message);
            throw new UnprocessableEntityException($"An error occurred while retrieving the business item. Please contact the system administrator");
        }
    }
}
