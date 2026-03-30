using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.ApproveBusinessItemPriceChange;

public class ApproveBusinessItemPriceChangeCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    ILogger<ApproveBusinessItemPriceChangeCommandHandler> logger)
    : IRequestHandler<ApproveBusinessItemPriceChangeCommand, ApproveBusinessItemPriceChangeResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<ApproveBusinessItemPriceChangeCommandHandler> _logger = logger;

    public async Task<ApproveBusinessItemPriceChangeResult> Handle(
        ApproveBusinessItemPriceChangeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var priceHistory = await _context.BusinessItemPriceHistories
                    .Include(ph => ph.BusinessItem)
                    .FirstOrDefaultAsync(ph => ph.Id == request.PriceHistoryId, cancellationToken);

                if (priceHistory is null)
                {
                    return new ApproveBusinessItemPriceChangeResult(
                        false,
                        "Price change request not found");
                }

                // Verify the user has access to this business
                if (priceHistory.BusinessItem.BusinessID != _currentUserService.BusinessId)
                {
                    return new ApproveBusinessItemPriceChangeResult(
                        false,
                        "Access denied to this price change request");
                }

                if (priceHistory.Status != ApprovalStatus.Pending)
                {
                    return new ApproveBusinessItemPriceChangeResult(
                        false,
                        $"This price change has already been {priceHistory.Status.ToString().ToLower()}");
                }

                // Approve the price change
                priceHistory.Approve(_currentUserService.UserId!.Value, request.Comments);

                // Apply the new price to the business item
                priceHistory.BusinessItem.ApplyApprovedPrice(priceHistory.NewPrice);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Price change approved for BusinessItem {BusinessItemId}: {OldPrice} -> {NewPrice} by ClientAdmin {UserId}",
                priceHistory.BusinessItemId, priceHistory.OldPrice, priceHistory.NewPrice, _currentUserService.UserId);

            return new ApproveBusinessItemPriceChangeResult(
                true,
                "Price change approved and applied successfully",
                priceHistory.NewPrice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving price change {PriceHistoryId}", request.PriceHistoryId);
            return new ApproveBusinessItemPriceChangeResult(false, $"Error approving price change: {ex.Message}");
        }
    }
}
