using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.SubscriptionKeys.Queries;

public class ValidateSubscriptionKeyQueryHandler : IRequestHandler<ValidateSubscriptionKeyQuery, ValidateSubscriptionKeyResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ValidateSubscriptionKeyQueryHandler> _logger;

    public ValidateSubscriptionKeyQueryHandler(
        IApplicationDbContext context,
        ILogger<ValidateSubscriptionKeyQueryHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidateSubscriptionKeyResult> Handle(ValidateSubscriptionKeyQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Validating subscription key: {Key}", request.Key);

            var subscriptionKey = await _context.SubscriptionKeys
                .Where(sk => sk.Key == request.Key && !sk.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (subscriptionKey == null)
            {
                _logger.LogWarning("Subscription key not found: {Key}", request.Key);
                return new ValidateSubscriptionKeyResult
                {
                    IsValid = false,
                    ValidationError = "Subscription key not found"
                };
            }

            if (!subscriptionKey.IsActive)
            {
                _logger.LogWarning("Subscription key is not active: {Key}", request.Key);
                return new ValidateSubscriptionKeyResult
                {
                    IsValid = false,
                    ValidationError = "Subscription key is not active"
                };
            }

            if (subscriptionKey.IsUsed)
            {
                _logger.LogWarning("Subscription key has already been used: {Key}", request.Key);
                return new ValidateSubscriptionKeyResult
                {
                    IsValid = false,
                    ValidationError = "Subscription key has already been used"
                };
            }

            if (subscriptionKey.ExpiryDate < DateTimeOffset.UtcNow)
            {
                _logger.LogWarning("Subscription key has expired: {Key}, Expiry: {ExpiryDate}", request.Key, subscriptionKey.ExpiryDate);
                return new ValidateSubscriptionKeyResult
                {
                    IsValid = false,
                    ValidationError = "Subscription key has expired"
                };
            }

            _logger.LogInformation("Subscription key is valid: {Key}, Business: {BusinessName}", request.Key, subscriptionKey.BusinessName);

            return new ValidateSubscriptionKeyResult
            {
                IsValid = true,
                SubscriptionKeyId = subscriptionKey.Id,
                BusinessName = subscriptionKey.BusinessName,
                ContactEmail = subscriptionKey.ContactEmail,
                ExpiryDate = subscriptionKey.ExpiryDate,
                MaxUsers = subscriptionKey.MaxUsers,
                MaxBusinesses = subscriptionKey.MaxBusinesses,
                Features = subscriptionKey.Features
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating subscription key: {Key}", request.Key);
            throw;
        }
    }
}