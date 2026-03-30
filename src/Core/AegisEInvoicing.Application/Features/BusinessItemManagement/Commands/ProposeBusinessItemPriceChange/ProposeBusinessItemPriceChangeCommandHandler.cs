using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.ProposeBusinessItemPriceChange;

public class ProposeBusinessItemPriceChangeCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    ILogger<ProposeBusinessItemPriceChangeCommandHandler> logger)
    : IRequestHandler<ProposeBusinessItemPriceChangeCommand, ProposeBusinessItemPriceChangeResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<ProposeBusinessItemPriceChangeCommandHandler> _logger = logger;

    public async Task<ProposeBusinessItemPriceChangeResult> Handle(
        ProposeBusinessItemPriceChangeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var businessItem = await _context.BusinessItems
                .Include(bi => bi.PriceHistory)
                .FirstOrDefaultAsync(bi => bi.Id == request.BusinessItemId
                    && bi.BusinessID == _currentUserService.BusinessId, cancellationToken);

            if (businessItem is null)
            {
                return new ProposeBusinessItemPriceChangeResult(
                    false,
                    "Business item not found or access denied");
            }

            if (businessItem.HasPendingPriceChange)
            {
                return new ProposeBusinessItemPriceChangeResult(
                    false,
                    "There is already a pending price change for this item. Please wait for approval or rejection.");
            }

            if (businessItem.UnitPrice == request.NewPrice)
            {
                return new ProposeBusinessItemPriceChangeResult(
                    false,
                    "The proposed price is the same as the current price");
            }

            var priceHistory = businessItem.ProposePrice(request.NewPrice, request.Comments);

            await _context.BusinessItemPriceHistories.AddAsync(priceHistory, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Price change proposed for BusinessItem {BusinessItemId}: {OldPrice} -> {NewPrice} by user {UserId}",
                businessItem.Id, businessItem.UnitPrice, request.NewPrice, _currentUserService.UserId);

            return new ProposeBusinessItemPriceChangeResult(
                true,
                "Price change proposed successfully. Awaiting ClientAdmin approval.",
                priceHistory.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proposing price change for BusinessItem {BusinessItemId}", request.BusinessItemId);
            return new ProposeBusinessItemPriceChangeResult(false, $"Error proposing price change: {ex.Message}");
        }
    }
}
