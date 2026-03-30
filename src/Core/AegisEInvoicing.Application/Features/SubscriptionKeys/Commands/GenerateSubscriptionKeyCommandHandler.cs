using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.SubscriptionKeys.Commands;

public class GenerateSubscriptionKeyCommandHandler : IRequestHandler<GenerateSubscriptionKeyCommand, GenerateSubscriptionKeyResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GenerateSubscriptionKeyCommandHandler> _logger;

    public GenerateSubscriptionKeyCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GenerateSubscriptionKeyCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GenerateSubscriptionKeyResult> Handle(GenerateSubscriptionKeyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating subscription key for business: {BusinessName}", request.BusinessName);

            // Validate that the user is authorized (should be KMPG admin)
            if (_currentUserService.UserId == null)
                throw new UnauthorizedAccessException("User not authenticated");

            // Check if there's already an active subscription key for this business
            var existingKey = await _context.SubscriptionKeys
                .Where(sk => sk.BusinessName == request.BusinessName && sk.IsActive && !sk.IsUsed)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingKey != null)
            {
                _logger.LogWarning("Active subscription key already exists for business: {BusinessName}", request.BusinessName);
                throw new InvalidOperationException($"An active subscription key already exists for business '{request.BusinessName}'");
            }

            // Create new subscription key
            var subscriptionKey = SubscriptionKey.Create(
                request.BusinessName,
                request.ContactEmail,
                request.ContactPhone,
                request.ExpiryDate,
                request.MaxUsers,
                request.MaxBusinesses,
                request.Features,
                _currentUserService.UserId.Value);

            _context.SubscriptionKeys.Add(subscriptionKey);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Subscription key generated successfully. ID: {SubscriptionKeyId}, Business: {BusinessName}", 
                subscriptionKey.Id, subscriptionKey.BusinessName);

            return new GenerateSubscriptionKeyResult
            {
                SubscriptionKeyId = subscriptionKey.Id,
                Key = subscriptionKey.Key,
                BusinessName = subscriptionKey.BusinessName,
                ExpiryDate = subscriptionKey.ExpiryDate,
                CreatedAt = subscriptionKey.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating subscription key for business: {BusinessName}", request.BusinessName);
            throw;
        }
    }
}