using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.ItemCategoryManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.ItemCategoryManagement.Commands.UpdateItemCategory;

public class UpdateItemCategoryCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<UpdateItemCategoryCommandHandler> logger) : IRequestHandler<UpdateItemCategoryCommand, ItemCategoryResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<UpdateItemCategoryCommandHandler> _logger = logger;

    public async Task<ItemCategoryResult> Handle(UpdateItemCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.UserId.HasValue)
                return new ItemCategoryResult(false, "User authentication required");

            if (!_currentUser.BusinessId.HasValue)
                return new ItemCategoryResult(false, "Business not Found");

            var itemCategory = await _context.ItemCategories.FirstOrDefaultAsync(i => i.Id == request.Id && i.BusinessID == _currentUser.BusinessId.Value, cancellationToken);

            if (itemCategory is null)
                return new ItemCategoryResult(false, $"Item Category does not exists.");

            itemCategory.UpdateDetails(request.Name, request.Description);

            itemCategory.MarkAsUpdated(_currentUser.UserId.Value);

            _context.ItemCategories.Update(itemCategory);
            await _context.SaveChangesAsync(cancellationToken);

            return new ItemCategoryResult(true, $"Item Category Updated Successfully.", itemCategory.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new ItemCategoryResult(
                false,
                $"Failed to update item category at this time. Please try again later.");
        }
    }
}