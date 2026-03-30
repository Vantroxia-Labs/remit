using AegisEInvoicing.Domain.Entities;

namespace AegisEInvoicing.Portal.API.Models.Business.Response;

/// <summary>
/// Response model for platform subscription information
/// </summary>
public class PlatformSubscriptionResponse
{
    /// <summary>
    /// Platform subscription unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the subscription plan
    /// </summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// Subscription tier (ApiOnly, SaaS, OnPremise)
    /// </summary>
    public SubscriptionTier Tier { get; set; }

    /// <summary>
    /// Monthly price for the subscription
    /// </summary>
    public double MonthlyPrice { get; set; }

    /// <summary>
    /// Currency for the subscription price
    /// </summary>
    public string Currency { get; set; } = string.Empty;
}
