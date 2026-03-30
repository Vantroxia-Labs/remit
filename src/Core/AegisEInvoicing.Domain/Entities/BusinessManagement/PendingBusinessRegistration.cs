using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Entities.BusinessManagement;

/// <summary>
/// Tracks a business registration that is pending payment confirmation.
/// Created when an admin initiates signup; activated after Paystack payment succeeds.
/// </summary>
public class PendingBusinessRegistration : AuditableEntity
{
    public string AdminFirstName { get; private set; } = null!;
    public string AdminLastName { get; private set; } = null!;
    public string AdminEmail { get; private set; } = null!;
    public string AdminPhone { get; private set; } = null!;
    public string BusinessName { get; private set; } = null!;
    public string? Tin { get; private set; }
    public Guid PlatformSubscriptionId { get; private set; }
    public BillingCycle BillingCycle { get; private set; }
    public PendingRegistrationStatus Status { get; private set; }
    public string PaystackReference { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public Guid? ActivatedBusinessId { get; private set; }

    private PendingBusinessRegistration() { } // EF constructor

    private PendingBusinessRegistration(
        string adminFirstName,
        string adminLastName,
        string adminEmail,
        string adminPhone,
        string businessName,
        string? tin,
        Guid platformSubscriptionId,
        BillingCycle billingCycle,
        string paystackReference)
    {
        AdminFirstName = adminFirstName;
        AdminLastName = adminLastName;
        AdminEmail = adminEmail;
        AdminPhone = adminPhone;
        BusinessName = businessName;
        Tin = tin;
        PlatformSubscriptionId = platformSubscriptionId;
        BillingCycle = billingCycle;
        PaystackReference = paystackReference;
        Status = PendingRegistrationStatus.AwaitingPayment;
        ExpiresAt = DateTimeOffset.UtcNow.AddHours(24); // Link expires in 24 hours
    }

    public static PendingBusinessRegistration Create(
        string adminFirstName,
        string adminLastName,
        string adminEmail,
        string adminPhone,
        string businessName,
        string? tin,
        Guid platformSubscriptionId,
        BillingCycle billingCycle,
        string paystackReference)
    {
        if (string.IsNullOrWhiteSpace(adminEmail))
            throw new ArgumentException("Admin email is required", nameof(adminEmail));
        if (string.IsNullOrWhiteSpace(businessName))
            throw new ArgumentException("Business name is required", nameof(businessName));

        return new PendingBusinessRegistration(
            adminFirstName,
            adminLastName,
            adminEmail,
            adminPhone,
            businessName,
            tin,
            platformSubscriptionId,
            billingCycle,
            paystackReference);
    }

    public void MarkPaid(DateTimeOffset paidAt)
    {
        Status = PendingRegistrationStatus.PaymentReceived;
        PaidAt = paidAt;
    }

    public void MarkActivated(Guid businessId)
    {
        Status = PendingRegistrationStatus.Activated;
        ActivatedBusinessId = businessId;
    }

    public void MarkFailed()
    {
        Status = PendingRegistrationStatus.Failed;
    }

    public bool IsExpired() => DateTimeOffset.UtcNow > ExpiresAt;
    public bool IsAwaitingPayment() => Status == PendingRegistrationStatus.AwaitingPayment && !IsExpired();
}

public enum PendingRegistrationStatus
{
    AwaitingPayment = 0,
    PaymentReceived = 1,
    Activated = 2,
    Failed = 3,
    Expired = 4
}

public enum BillingCycle
{
    Monthly = 0,
    Annual = 1
}
