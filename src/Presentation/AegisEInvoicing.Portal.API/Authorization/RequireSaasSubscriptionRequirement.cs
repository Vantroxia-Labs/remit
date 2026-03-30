using Microsoft.AspNetCore.Authorization;

namespace AegisEInvoicing.Portal.API.Authorization;

/// <summary>
/// Authorization requirement that enforces SaaS subscription tier for Create/Update/Delete operations
/// Only users with SubscriptionTier = "SaaS" can perform CUD operations in the Portal API
/// ApiOnly and SFTP tiers have read-only access
/// </summary>
public class RequireSaasSubscriptionRequirement : IAuthorizationRequirement
{
    // No properties needed - just marker interface
}
