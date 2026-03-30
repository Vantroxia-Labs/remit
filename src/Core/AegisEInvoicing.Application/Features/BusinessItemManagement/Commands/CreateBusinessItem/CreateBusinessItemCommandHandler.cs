using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Exceptions;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.CreateBusinessItem;

public class CreateBusinessItemCommandHandler : IRequestHandler<CreateBusinessItemCommand, BusinessItemResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateBusinessItemCommandHandler> _logger;

    public CreateBusinessItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<CreateBusinessItemCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<BusinessItemResult> Handle(CreateBusinessItemCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            _logger.LogWarning("Unauthorized attempt to create business item");
            throw new AuthenticationException("User authentication required");
        }

        if (!_currentUser.BusinessId.HasValue)
        {
            _logger.LogWarning("Business not found");
            throw new ForbiddenException("Business not found");
        }

        // Verify business exists
        var businessExists = await _context.Businesses
            .AnyAsync(b => b.Id == _currentUser.BusinessId.Value, cancellationToken);

        if (!businessExists)
        {
            _logger.LogWarning("Attempt to create business item for non-existent business {BusinessId}", _currentUser.BusinessId.Value);
            throw new NotFoundException("Business not found");
        }

        // Verify item category exists
        var categoryExists = await _context.ItemCategories
            .AnyAsync(ic => ic.Id == request.ItemCategoryId, cancellationToken);

        if (!categoryExists)
        {
            _logger.LogWarning("Attempt to create business item for non-existent item category {ItemCategoryId}", request.ItemCategoryId);
            throw new ConflictException("Item category not found");
        }

        // Check for duplicate business item name within the same business
        var existingItem = await _context.BusinessItems
            .Where(bi => bi.BusinessID == _currentUser.BusinessId.Value &&
                        bi.Name == request.Name &&
                        !bi.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingItem != null)
        {
            _logger.LogWarning("Attempt to create duplicate business item with name '{Name}' for business {BusinessId}", request.Name, _currentUser.BusinessId.Value);
            throw new ConflictException($"A business item with the name '{request.Name}' already exists for this business. Please use a different name.");
        }

        // Create value objects
        var serviceCode = ServiceCode.Create(request.ServiceCode.Code, request.ServiceCode.Name);
        var taxCategory = TaxCategory.Create(request.TaxCategory.Name, request.TaxCategory.Percent);

        // Create BusinessItem entity
        var businessItem = BusinessItem.Create(
            _currentUser.BusinessId.Value,
            request.Name,
            serviceCode,
            taxCategory,
            request.ItemCategoryId,
            request.ItemDescription,
            request.UnitPrice);

        // Save to database
        await _context.BusinessItems.AddAsync(businessItem);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created business item {BusinessItemId} for business {BusinessId}", businessItem.Id, _currentUser.BusinessId.Value);
        return new BusinessItemResult(true, "Successfully created business item", businessItem.Id);
    }
}