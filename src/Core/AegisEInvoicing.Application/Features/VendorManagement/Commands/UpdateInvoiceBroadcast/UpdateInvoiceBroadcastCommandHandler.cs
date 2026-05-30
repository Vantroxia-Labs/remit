using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.UpdateInvoiceBroadcast;

public class UpdateInvoiceBroadcastCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<UpdateInvoiceBroadcastCommandHandler> logger) : IRequestHandler<UpdateInvoiceBroadcastCommand, InvoiceBroadcastResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<UpdateInvoiceBroadcastCommandHandler> _logger = logger;

    public async Task<InvoiceBroadcastResult> Handle(UpdateInvoiceBroadcastCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new InvoiceBroadcastResult(false, "Unauthorized");

            var broadcast = await _context.InvoiceBroadcasts
                .FirstOrDefaultAsync(b => b.Id == request.Id && b.BusinessId == _currentUser.BusinessId.Value, cancellationToken);

            if (broadcast is null)
                return new InvoiceBroadcastResult(false, "Broadcast not found.");

            if (broadcast.Status == BroadcastStatus.Deactivated)
                return new InvoiceBroadcastResult(false, "Cannot edit a deactivated broadcast.");

            if (broadcast.IsExpired())
                return new InvoiceBroadcastResult(false, "Cannot edit an expired broadcast.");

            broadcast.Update(request.Title, broadcast.DueDate, request.Note);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Broadcast {BroadcastId} updated", broadcast.Id);
            return new InvoiceBroadcastResult(true, "Broadcast updated successfully.", broadcast.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating broadcast {BroadcastId}", request.Id);
            return new InvoiceBroadcastResult(false, "An error occurred while updating the broadcast.");
        }
    }
}
