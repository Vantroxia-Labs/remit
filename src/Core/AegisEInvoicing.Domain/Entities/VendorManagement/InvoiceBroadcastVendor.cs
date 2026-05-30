using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;

namespace AegisEInvoicing.Domain.Entities.VendorManagement;

public class InvoiceBroadcastVendor : AuditableEntity
{
    public Guid InvoiceBroadcastId { get; private set; }
    public Guid VendorId { get; private set; }
    public Guid? InvoiceId { get; private set; }

    // Email verification
    public string Token { get; private set; } = null!;
    public bool IsEmailVerified { get; private set; }
    public string? VerificationCode { get; private set; }
    public DateTimeOffset? VerificationCodeExpiresAt { get; private set; }
    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    public InvoiceBroadcast InvoiceBroadcast { get; private set; } = null!;
    public Vendor Vendor { get; private set; } = null!;
    public Invoice? Invoice { get; private set; }

    private InvoiceBroadcastVendor() { }

    public static InvoiceBroadcastVendor Create(Guid broadcastId, Guid vendorId)
    {
        if (broadcastId == Guid.Empty)
            throw new ArgumentException("Broadcast ID is required", nameof(broadcastId));

        if (vendorId == Guid.Empty)
            throw new ArgumentException("Vendor ID is required", nameof(vendorId));

        return new InvoiceBroadcastVendor
        {
            InvoiceBroadcastId = broadcastId,
            VendorId = vendorId,
            Token = Guid.NewGuid().ToString("N"),
            IsEmailVerified = false
        };
    }

    public void SetVerificationCode(string code, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Verification code is required", nameof(code));

        VerificationCode = code;
        VerificationCodeExpiresAt = expiresAt;
        IsEmailVerified = false;
    }

    public void MarkEmailVerified()
    {
        IsEmailVerified = true;
        EmailVerifiedAt = DateTimeOffset.UtcNow;
        VerificationCode = null;
        VerificationCodeExpiresAt = null;
    }

    public void AssignInvoice(Guid invoiceId)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("Invoice ID is required", nameof(invoiceId));

        InvoiceId = invoiceId;
    }

    public bool IsVerificationCodeValid(string code)
    {
        return VerificationCode == code
            && VerificationCodeExpiresAt.HasValue
            && VerificationCodeExpiresAt.Value > DateTimeOffset.UtcNow;
    }
}
