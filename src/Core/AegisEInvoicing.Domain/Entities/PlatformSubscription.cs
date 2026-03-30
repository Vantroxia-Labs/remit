using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Events.PlatformSubscription;

namespace AegisEInvoicing.Domain.Entities;

/// <summary>
/// Represents a subscription for a business in the SaaS platform
/// Controls access to platform features and billing
/// </summary>
public class PlatformSubscription : AuditableAggregateRoot 
{
    public string PlanName { get; private set; }
    public SubscriptionTier Tier { get; private set; }
    public double MonthlyPrice { get; private set; }
    public double AnnualPrice { get; private set; }
    public string Currency { get; set; } = "NGN";
    public string Description => $"{PlanName} - {Tier} - {MonthlyPrice:C} {Currency}";

    private readonly List<Subscription> _subscriptions = [];
    public IReadOnlyCollection<Subscription> Subscriptions => _subscriptions.AsReadOnly();

    private PlatformSubscription()
    {
        PlanName = string.Empty;
    }

    private PlatformSubscription(
        string planName,
        SubscriptionTier tier,
        double monthlyPrice,
        double annualPrice,
        string currency = "NGN")
    {
        PlanName = planName;
        Tier = tier;
        MonthlyPrice = monthlyPrice;
        AnnualPrice = annualPrice;
        Currency = currency;
    }

    public static PlatformSubscription Create(
        string planName,
        SubscriptionTier tier,
        double monthlyPrice,
        Guid createdBy,
        double? annualPrice = null)
    {
        ValidateInputs(planName, monthlyPrice);

        var platformSubscription = new PlatformSubscription(
            planName,
            tier,
            monthlyPrice,
            annualPrice ?? monthlyPrice * 12,
            "NGN");

        platformSubscription.AddDomainEvent(new PlatformSubscriptionCreatedEvent(
            platformSubscription.Id,
            platformSubscription.PlanName,
            platformSubscription.Tier,
            createdBy,
            DateTimeOffset.UtcNow));

        return platformSubscription;
    }

    public void Update(
        string planName,
        SubscriptionTier tier,
        double monthlyPrice,
        Guid updatedBy,
        string? currency = null)
    {
        ValidateInputs(planName, monthlyPrice);

        var hasChanges = false;
        var oldPlanName = PlanName;
        var oldTier = Tier;
        var oldMonthlyPrice = MonthlyPrice;
        var oldCurrency = Currency;

        if (PlanName != planName)
        {
            PlanName = planName;
            hasChanges = true;
        }

        if (Tier != tier)
        {
            Tier = tier;
            hasChanges = true;
        }

        if (Math.Abs(MonthlyPrice - monthlyPrice) > 0.01) // Use epsilon comparison for double
        {
            MonthlyPrice = monthlyPrice;
            hasChanges = true;
        }

        if (currency != null && Currency != currency)
        {
            Currency = currency;
            hasChanges = true;
        }

        // Only raise domain event if there are actual changes
        if (hasChanges)
        {
            AddDomainEvent(new PlatformSubscriptionUpdatedEvent(
                Id,
                planName,
                tier,
                monthlyPrice,
                currency ?? Currency,
                updatedBy,
                DateTimeOffset.UtcNow,
                oldPlanName,
                oldTier,
                oldMonthlyPrice,
                oldCurrency));
        }
    }

    public void Delete(Guid deletedBy, bool force = false)
    {
        // Prevent deletion if already deleted
        if (IsDeleted)
        {
            throw new InvalidOperationException("Platform subscription is already deleted");
        }

        // Check for active subscriptions unless forced
        if (!force && _subscriptions.Any(s => !s.IsDeleted)) // Assuming Subscription has IsDeleted property
        {
            throw new InvalidOperationException(
                "Cannot delete platform subscription with active subscriptions. " +
                "Please delete or migrate all subscriptions first, or use force delete.");
        }

        // Perform soft delete
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;

        // Raise domain event
        AddDomainEvent(new PlatformSubscriptionDeletedEvent(
            Id,
            PlanName,
            Tier,
            deletedBy,
            DeletedAt.Value,
            _subscriptions.Count));
    }

    private static void ValidateInputs(string planName, double monthlyPrice)
    {
        if (string.IsNullOrWhiteSpace(planName))
            throw new ArgumentException("Plan name is required", nameof(planName));

        if (monthlyPrice < 0)
            throw new ArgumentException("Monthly price cannot be negative", nameof(monthlyPrice));
    }
}

public enum SubscriptionTier
{
    ApiOnly = 0,    // API-only access, Portal read-only
    SaaS = 1,       // Full SaaS platform access (Portal CRUD + API)
    SFTP = 2        // SFTP integration, Portal read-only
}