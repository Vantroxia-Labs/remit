using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.RejectBusinessItemPriceChange;

public class RejectBusinessItemPriceChangeCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    ILogger<RejectBusinessItemPriceChangeCommandHandler> logger)
    : IRequestHandler<RejectBusinessItemPriceChangeCommand, RejectBusinessItemPriceChangeResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<RejectBusinessItemPriceChangeCommandHandler> _logger = logger;

    public async Task<RejectBusinessItemPriceChangeResult> Handle(
        RejectBusinessItemPriceChangeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var priceHistory = await _context.BusinessItemPriceHistories
                .Include(ph => ph.BusinessItem)
                .FirstOrDefaultAsync(ph => ph.Id == request.PriceHistoryId, cancellationToken);

            if (priceHistory is null)
            {
                return new RejectBusinessItemPriceChangeResult(
                    false,
                    "Price change request not found");
            }

            // Verify the user has access to this business
            if (priceHistory.BusinessItem.BusinessID != _currentUserService.BusinessId)
            {
                return new RejectBusinessItemPriceChangeResult(
                    false,
                    "Access denied to this price change request");
            }

            if (priceHistory.Status != ApprovalStatus.Pending)
            {
                return new RejectBusinessItemPriceChangeResult(
                    false,
                    $"This price change has already been {priceHistory.Status.ToString().ToLower()}");
            }

            // Reject the price change
            priceHistory.Reject(_currentUserService.UserId!.Value, request.Comments);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Price change rejected for BusinessItem {BusinessItemId}: {OldPrice} -> {NewPrice} (rejected) by ClientAdmin {UserId}",
                priceHistory.BusinessItemId, priceHistory.OldPrice, priceHistory.NewPrice, _currentUserService.UserId);

            return new RejectBusinessItemPriceChangeResult(
                true,
                "Price change rejected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting price change {PriceHistoryId}", request.PriceHistoryId);
            return new RejectBusinessItemPriceChangeResult(false, $"Error rejecting price change: {ex.Message}");
        }
    }
}
