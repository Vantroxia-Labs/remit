using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.ItemCategoryManagement.DTOs;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.NotificationService.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.ItemCategoryManagement.Commands.CreateItemCategory;

public class CreateItemCategoryCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEmailService emailService,
    ILogger<CreateItemCategoryCommandHandler> logger) : IRequestHandler<CreateItemCategoryCommand, ItemCategoryResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<CreateItemCategoryCommandHandler> _logger = logger;

    public async Task<ItemCategoryResult> Handle(CreateItemCategoryCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return new ItemCategoryResult(false, "User authentication required");

        if (!_currentUser.BusinessId.HasValue)
            return new ItemCategoryResult(false, "Business not Found");

        var itemCategory = ItemCategory.Create(
            request.Name,
            request.Description,
            _currentUser.BusinessId.Value);

        itemCategory.MarkAsCreated(_currentUser.UserId.Value);

        var createdCategory = await _context.ItemCategories.AddAsync(itemCategory, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        if (createdCategory is null)
        {
            return new ItemCategoryResult(false,
                           $"We could not create the Item category");
        }

        return new ItemCategoryResult(true,
                                     $"We have successfully created the Item category",
                                     itemCategory.Id);
    }
}
