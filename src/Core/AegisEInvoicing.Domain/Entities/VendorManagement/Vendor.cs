using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Domain.Entities.VendorManagement;

public class Vendor : AuditableAggregateRoot
{
    public string BusinessName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    public VendorStatus Status { get; private set; } = VendorStatus.Active;
    public Guid BusinessId { get; private set; }
    public Guid VendorGroupId { get; private set; }

    public Business Business { get; private set; } = null!;
    public VendorGroup VendorGroup { get; private set; } = null!;

    private readonly List<InvoiceBroadcastVendor> _broadcastVendors = [];
    public IReadOnlyCollection<InvoiceBroadcastVendor> BroadcastVendors => _broadcastVendors.AsReadOnly();

    private Vendor() { }

    public static Vendor Create(
        string businessName,
        string email,
        Guid vendorGroupId,
        Guid businessId,
        string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(businessName))
            throw new ArgumentException("Business name is required", nameof(businessName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        if (vendorGroupId == Guid.Empty)
            throw new ArgumentException("Vendor group is required", nameof(vendorGroupId));

        if (businessId == Guid.Empty)
            throw new ArgumentException("Business ID is required", nameof(businessId));

        return new Vendor
        {
            BusinessName = businessName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone?.Trim(),
            VendorGroupId = vendorGroupId,
            BusinessId = businessId,
            Status = VendorStatus.Active
        };
    }

    public void Update(string businessName, string email, Guid vendorGroupId, string? phone)
    {
        if (string.IsNullOrWhiteSpace(businessName))
            throw new ArgumentException("Business name is required", nameof(businessName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        if (vendorGroupId == Guid.Empty)
            throw new ArgumentException("Vendor group is required", nameof(vendorGroupId));

        BusinessName = businessName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone?.Trim();
        VendorGroupId = vendorGroupId;
    }

    public void Activate() => Status = VendorStatus.Active;
    public void Deactivate() => Status = VendorStatus.Inactive;
}
