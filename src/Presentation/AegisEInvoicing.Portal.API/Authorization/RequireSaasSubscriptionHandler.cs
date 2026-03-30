using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AegisEInvoicing.Portal.API.Authorization;

/// <summary>
/// Authorization handler that enforces SaaS subscription tier requirement
/// Validates that the user's SubscriptionTier claim equals "SaaS"
/// </summary>
public class RequireSaasSubscriptionHandler(ILogger<RequireSaasSubscriptionHandler> logger) : AuthorizationHandler<RequireSaasSubscriptionRequirement>
{
    private readonly ILogger<RequireSaasSubscriptionHandler> _logger = logger;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequireSaasSubscriptionRequirement requirement)
    {
        // Get SubscriptionTier from JWT claims
        var subscriptionTierClaim = context.User.FindFirst("SubscriptionTier")?.Value;

        if (string.IsNullOrEmpty(subscriptionTierClaim))
        {
            _logger.LogWarning(
                "Authorization failed: SubscriptionTier claim not found for user {UserId}",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            // Fail - no subscription tier claim
            context.Fail();
            return Task.CompletedTask;
        }

        // Only "SaaS" tier is allowed for CUD operations
        if (subscriptionTierClaim == "SaaS")
        {
            _logger.LogDebug(
                "Authorization succeeded: User {UserId} has SaaS subscription tier",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Authorization failed: User {UserId} has {SubscriptionTier} tier (requires SaaS for write operations)",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                subscriptionTierClaim);
            
            // Fail - ApiOnly or SFTP tier (read-only)
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
