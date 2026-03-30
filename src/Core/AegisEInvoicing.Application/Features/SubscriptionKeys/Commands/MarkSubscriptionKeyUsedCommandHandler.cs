using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.SubscriptionKeys.Commands;

public class MarkSubscriptionKeyUsedCommandHandler : IRequestHandler<MarkSubscriptionKeyUsedCommand, MarkSubscriptionKeyUsedResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MarkSubscriptionKeyUsedCommandHandler> _logger;

    public MarkSubscriptionKeyUsedCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<MarkSubscriptionKeyUsedCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MarkSubscriptionKeyUsedResult> Handle(MarkSubscriptionKeyUsedCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var subscriptionKey = await _context.SubscriptionKeys
                .FirstOrDefaultAsync(sk => sk.Id == request.SubscriptionKeyId, cancellationToken);

            if (subscriptionKey == null)
            {
                return new MarkSubscriptionKeyUsedResult
                {
                    Success = false,
                    Message = "Subscription key not found"
                };
            }

            if (subscriptionKey.IsUsed)
            {
                return new MarkSubscriptionKeyUsedResult
                {
                    Success = false,
                    Message = "Subscription key has already been used"
                };
            }

            var userId = _currentUserService.UserId ?? Guid.CreateVersion7(); // Fallback for system operations
            subscriptionKey.MarkAsUsed(userId, request.Notes);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Subscription key marked as used: {SubscriptionKeyId}", request.SubscriptionKeyId);

            return new MarkSubscriptionKeyUsedResult
            {
                Success = true,
                Message = "Subscription key marked as used successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking subscription key as used: {SubscriptionKeyId}", request.SubscriptionKeyId);
            throw;
        }
    }
}