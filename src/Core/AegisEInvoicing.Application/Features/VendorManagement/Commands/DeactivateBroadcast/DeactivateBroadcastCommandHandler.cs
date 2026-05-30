using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.DeactivateBroadcast;

public class DeactivateBroadcastCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<DeactivateBroadcastCommandHandler> logger) : IRequestHandler<DeactivateBroadcastCommand, DeactivateBroadcastResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<DeactivateBroadcastCommandHandler> _logger = logger;

    public async Task<DeactivateBroadcastResult> Handle(DeactivateBroadcastCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new DeactivateBroadcastResult(false, "Unauthorized");

            var broadcast = await _context.InvoiceBroadcasts
                .FirstOrDefaultAsync(b => b.Id == request.Id && b.BusinessId == _currentUser.BusinessId.Value, cancellationToken);

            if (broadcast is null)
                return new DeactivateBroadcastResult(false, "Broadcast not found.");

            // Check for pending invoices
            var hasPendingInvoices = await _context.InvoiceBroadcastVendors
                .AnyAsync(bv => bv.InvoiceBroadcastId == request.Id && bv.InvoiceId != null, cancellationToken);

            broadcast.Deactivate();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Broadcast {BroadcastId} deactivated", broadcast.Id);
            return new DeactivateBroadcastResult(true, "Broadcast deactivated.", hasPendingInvoices);
        }
        catch (InvalidOperationException ioEx)
        {
            return new DeactivateBroadcastResult(false, ioEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating broadcast {BroadcastId}", request.Id);
            return new DeactivateBroadcastResult(false, "An error occurred while deactivating the broadcast.");
        }
    }
}
