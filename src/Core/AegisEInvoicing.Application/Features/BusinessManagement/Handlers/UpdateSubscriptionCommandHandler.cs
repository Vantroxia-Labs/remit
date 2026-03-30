using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Handlers;

public class UpdateSubscriptionCommandHandler : IRequestHandler<UpdateSubscriptionCommand, UpdateSubscriptionResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateSubscriptionCommandHandler> _logger;

    public UpdateSubscriptionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdateSubscriptionCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UpdateSubscriptionResult> Handle(UpdateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new InvalidOperationException("Current user ID is not available");

            var business = await _context.Businesses
                .Include(b => b.Subscription)
                .FirstOrDefaultAsync(b => b.Id == request.BusinessId, cancellationToken);

            if (business == null)
            {
                return new UpdateSubscriptionResult
                {
                    Success = false,
                    Message = $"Business not found: {request.BusinessId}"
                };
            }

            // Verify the new subscription exists
            var newSubscription = await _context.PlatformSubscriptions
                .FirstOrDefaultAsync(s => s.Id == request.PlatformSubscriptionId, cancellationToken);

            if (newSubscription == null)
            {
                return new UpdateSubscriptionResult
                {
                    Success = false,
                    Message = $"Platform subscription not found: {request.PlatformSubscriptionId}"
                };
            }

            // Update the business subscription
            if (business.SubscriptionId.HasValue)
            {
                business.UpdateSubscription(request.PlatformSubscriptionId, currentUserId);
            }
            else
            {
                business.AssignSubscription(request.PlatformSubscriptionId, currentUserId);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated subscription for business {BusinessId} to subscription {SubscriptionId}",
                request.BusinessId, request.PlatformSubscriptionId);

            return new UpdateSubscriptionResult
            {
                Success = true,
                Message = "Subscription updated successfully"
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business operation error for business {BusinessId}: {Message}",
                request.BusinessId, ex.Message);
            return new UpdateSubscriptionResult
            {
                Success = false,
                Message = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription for business: {BusinessId}", request.BusinessId);
            return new UpdateSubscriptionResult
            {
                Success = false,
                Message = "Failed to update subscription"
            };
        }
    }
}
