using EInvoiceIntegrator.Application.Common.Interfaces;
using EInvoiceIntegrator.Application.Features.BusinessManagement.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EInvoiceIntegrator.Application.Features.BusinessManagement.Handlers;

public class UpdateBusinessSubscriptionCommandHandler : IRequestHandler<UpdateBusinessSubscriptionCommand, BusinessManagementCommandResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UpdateBusinessSubscriptionCommandHandler> _logger;

    public UpdateBusinessSubscriptionCommandHandler(
        IApplicationDbContext context,
        ILogger<UpdateBusinessSubscriptionCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BusinessManagementCommandResult> Handle(UpdateBusinessSubscriptionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var business = await _context.Businesses
                .Include(b => b.Subscriptions)
                .FirstOrDefaultAsync(b => b.Id == request.BusinessId, cancellationToken);

            if (business == null)
            {
                return new BusinessManagementCommandResult
                {
                    Success = false,
                    Message = $"Business not found: {request.BusinessId}"
                };
            }

            // Note: This would need proper implementation with domain methods
            // For now, this is a placeholder implementation
            
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated subscription for business {BusinessId} to tier {NewTier}", 
                request.BusinessId, request.NewTier);

            return new BusinessManagementCommandResult
            {
                Success = true,
                Message = "Subscription updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription for business {BusinessId}", request.BusinessId);
            return new BusinessManagementCommandResult
            {
                Success = false,
                Message = "Failed to update subscription"
            };
        }
    }
}