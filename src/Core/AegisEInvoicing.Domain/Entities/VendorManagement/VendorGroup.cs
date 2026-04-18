using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.BusinessManagement;

namespace AegisEInvoicing.Domain.Entities.VendorManagement;

public class VendorGroup : AuditableAggregateRoot
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid BusinessId { get; private set; }

    public Business Business { get; private set; } = null!;

    private readonly List<Vendor> _vendors = [];
    public IReadOnlyCollection<Vendor> Vendors => _vendors.AsReadOnly();

    private VendorGroup() { }

    public static VendorGroup Create(string name, string? description, Guid businessId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name is required", nameof(name));

        if (businessId == Guid.Empty)
            throw new ArgumentException("Business ID is required", nameof(businessId));

        return new VendorGroup
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            BusinessId = businessId
        };
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name is required", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
    }
}
