using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using AegisEInvoicing.Application.Features.UserManagement.Queries.GetPlatformRoles;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Exceptions;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.UpdateBusinessItem;

public class UpdateBusinessItemCommandHandler : IRequestHandler<UpdateBusinessItemCommand, BusinessItemResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UpdateBusinessItemCommandHandler> _logger;

    public UpdateBusinessItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<UpdateBusinessItemCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<BusinessItemResult> Handle(UpdateBusinessItemCommand request, CancellationToken cancellationToken)
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
            .FirstOrDefaultAsync(bi => bi.Id == request.Id
         && bi.BusinessID == _currentUser.BusinessId.Value,
         cancellationToken);

        if (businessItem is null)
        {
            _logger.LogWarning("Attempt to update non-existent business item {BusinessItemId}", request.Id);
            throw new NotFoundException("Business item not found");
        }

        var serviceCode = ServiceCode.Create(request.ServiceCode.Code, request.ServiceCode.Name);

        // Update non-price properties
        businessItem.Update(
            request.Name,
            request.ItemType,
            serviceCode,
            Guid.Empty,
            request.ItemDescription);

        // Update tax categories
        var taxCategories = request.TaxCategories.Select(tc =>
            tc.IsPercentage
                ? BusinessItemTaxCategory.CreatePercentage(tc.Code, tc.Name, tc.Percent!.Value)
                : BusinessItemTaxCategory.CreateFlatFee(tc.Code, tc.Name, tc.FlatAmount!.Value)).ToList();
        businessItem.UpdateTaxCategories(taxCategories);

        // Handle price change separately - requires approval
        if ((businessItem.UnitPrice != request.UnitPrice) && !_currentUser.Roles.Contains(RoleConstants.ClientAdmin))
        {
            var priceHistory = businessItem.ProposePrice(request.UnitPrice, "Price update requested via BusinessItem update");
            await _context.BusinessItemPriceHistories.AddAsync(priceHistory, cancellationToken);
            _logger.LogInformation("Price change proposed for BusinessItem {BusinessItemId}: {OldPrice} -> {NewPrice}",
                businessItem.Id, businessItem.UnitPrice, request.UnitPrice);
        }
        else
        {
            businessItem.ApplyApprovedPrice(request.UnitPrice);
        }

        _context.BusinessItems.Update(businessItem);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully updated business item {BusinessItemId}", request.Id);
        return new BusinessItemResult(true, "Successfully updated business item", request.Id);
    }
}