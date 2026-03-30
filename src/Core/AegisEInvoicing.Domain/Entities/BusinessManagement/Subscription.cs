using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Events.UserManagement;

namespace AegisEInvoicing.Domain.Entities.BusinessManagement;

/// <summary>
/// Represents a subscription for a business in the SaaS platform
/// Controls access to platform features and billing
/// </summary>
public class Subscription : AuditableAggregateRoot
{
    public Guid BusinessId { get; private set; }
    public Guid PlatformSubscriptionId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset EndDate { get; private set; }
    public DateTimeOffset? LastBillingDate { get; private set; }
    public DateTimeOffset? NextBillingDate { get; private set; }

    // Navigation properties
    public Business Business { get; private set; } = null!;
    public PlatformSubscription PlatformSubscription { get; private set; } = null!;

    // Parameterless constructor for Entity Framework
    private Subscription()
    {
        Status = SubscriptionStatus.Active;
    }

    private Subscription(
        Guid businessId,
        Guid platformSubscriptionId,
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        BusinessId = businessId;
        PlatformSubscriptionId = platformSubscriptionId;
        StartDate = startDate;
        EndDate = endDate;
        Status = SubscriptionStatus.Active;
    }

    public static Subscription Create(
        Guid businessId,
        Guid platformSubscriptionId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        Guid createdBy)
    {

        var subscription = new Subscription(
            businessId,
            platformSubscriptionId,
            startDate,
            endDate);

        subscription.AddDomainEvent(new SubscriptionCreatedEvent(
            subscription.Id,
            subscription.BusinessId,
            subscription.PlatformSubscriptionId,
            subscription.StartDate,
            subscription.EndDate,
            createdBy,
            DateTimeOffset.UtcNow));

        return subscription;
    }

    public void Activate(Guid activatedBy)
    {
        if (Status == SubscriptionStatus.Active)
            throw new InvalidOperationException("Subscription is already active");

        Status = SubscriptionStatus.Active;
        AddDomainEvent(new SubscriptionActivatedEvent(Id, BusinessId, PlatformSubscriptionId, activatedBy, DateTimeOffset.UtcNow));
    }

    public void Suspend(Guid suspendedBy, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Suspension reason is required", nameof(reason));

        if (Status == SubscriptionStatus.Suspended)
            throw new InvalidOperationException("Subscription is already suspended");

        Status = SubscriptionStatus.Suspended;
        AddDomainEvent(new SubscriptionSuspendedEvent(Id, BusinessId, PlatformSubscriptionId, suspendedBy, reason, DateTimeOffset.UtcNow));
    }

    public void Expire(Guid expiredBy)
    {
        if (Status == SubscriptionStatus.Expired)
            throw new InvalidOperationException("Subscription is already expired");

        Status = SubscriptionStatus.Expired;
        AddDomainEvent(new SubscriptionExpiredEvent(Id, BusinessId, PlatformSubscriptionId, expiredBy, DateTimeOffset.UtcNow));
    }

    public void Cancel(Guid cancelledBy, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required", nameof(reason));

        if (Status == SubscriptionStatus.Cancelled)
            throw new InvalidOperationException("Subscription is already cancelled");

        Status = SubscriptionStatus.Cancelled;
        AddDomainEvent(new SubscriptionCancelledEvent(Id, BusinessId, PlatformSubscriptionId, cancelledBy, reason, DateTimeOffset.UtcNow));
    }

    public void Renew(DateTimeOffset newEndDate, Guid renewedBy)
    {
        if (newEndDate <= EndDate)
            throw new ArgumentException("New end date must be after current end date", nameof(newEndDate));

        var oldEndDate = EndDate;
        EndDate = newEndDate;
        Status = SubscriptionStatus.Active;

        AddDomainEvent(new SubscriptionRenewedEvent(
            Id, 
            BusinessId, 
            PlatformSubscriptionId, 
            oldEndDate, 
            newEndDate, 
            renewedBy, 
            DateTimeOffset.UtcNow));
    }

    public void UpdateBilling(DateTimeOffset billingDate, DateTimeOffset nextBillingDate)
    {
        LastBillingDate = billingDate;
        NextBillingDate = nextBillingDate;
    }

    public void UpdateBilling(DateTimeOffset billingDate, int duration, Guid updatedBy)
    {
        LastBillingDate = billingDate;
        NextBillingDate = billingDate.AddMonths(duration - 1);

        AddDomainEvent(new SubscriptionBilledEvent(Id, BusinessId, PlatformSubscriptionId, billingDate, PlatformSubscription.MonthlyPrice, PlatformSubscription.Currency, updatedBy, DateTimeOffset.UtcNow));
    }

    // Status check methods
    public bool IsActive() => Status == SubscriptionStatus.Active && !IsExpired();

    public bool IsExpired() => DateTimeOffset.UtcNow > EndDate;


    public bool IsGracePeriod() => IsExpired() && DateTimeOffset.UtcNow <= EndDate.AddDays(7); // 7-day grace period

    public int DaysUntilExpiry() => Math.Max(0, (EndDate - DateTimeOffset.UtcNow).Days);

    public int DaysOverdue() => IsExpired() ? (DateTimeOffset.UtcNow - EndDate).Days : 0;
}

public enum SubscriptionStatus
{
    Active,
    Suspended,
    Expired,
    Cancelled,
    PendingPayment
}