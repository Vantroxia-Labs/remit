using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.VendorManagement.Commands.ExtendBroadcastDueDate;

public class ExtendBroadcastDueDateCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<ExtendBroadcastDueDateCommandHandler> logger) : IRequestHandler<ExtendBroadcastDueDateCommand, InvoiceBroadcastResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<ExtendBroadcastDueDateCommandHandler> _logger = logger;

    public async Task<InvoiceBroadcastResult> Handle(ExtendBroadcastDueDateCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new InvoiceBroadcastResult(false, "Unauthorized");

            var broadcast = await _context.InvoiceBroadcasts
                .FirstOrDefaultAsync(b => b.Id == request.Id && b.BusinessId == _currentUser.BusinessId.Value, cancellationToken);

            if (broadcast is null)
                return new InvoiceBroadcastResult(false, "Broadcast not found.");

            broadcast.ExtendDueDate(request.NewDueDate);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Broadcast {BroadcastId} due date extended to {NewDate}", broadcast.Id, request.NewDueDate);
            return new InvoiceBroadcastResult(true, $"Due date extended to {request.NewDueDate:dd MMM yyyy}.", broadcast.Id);
        }
        catch (ArgumentException argEx)
        {
            return new InvoiceBroadcastResult(false, argEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending broadcast due date {BroadcastId}", request.Id);
            return new InvoiceBroadcastResult(false, "An error occurred while extending the due date.");
        }
    }
}
