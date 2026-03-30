using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.ValueObjects;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Entities.BusinessManagement;

namespace AegisEInvoicing.Domain.Entities;

public class Branch : AuditableEntity
{
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public Address Address { get; private set; } = null!;
    public string ContactEmail { get; private set; } = string.Empty;
    public string ContactPhone { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsHeadOffice { get; private set; }
    public Guid? AdminUserId { get; private set; }

    // Navigation properties
    public Business Business { get; private set; } = null!;
    public User? AdminUser { get; private set; }

    private readonly List<User> _users = [];
    public IReadOnlyCollection<User> Users => _users.AsReadOnly();

    private Branch() { } // EF Constructor

    public static Branch Create(
        Guid businessId,
        string name,
        string code,
        Address address,
        string contactEmail,
        string contactPhone,
        Guid createdBy,
        bool isHeadOffice = false,
        Guid? adminUserId = null)
    {
        return new Branch
        {
            Id = Guid.CreateVersion7(),
            BusinessId = businessId,
            Name = name,
            Code = code,
            Address = address,
            ContactEmail = contactEmail,
            ContactPhone = contactPhone,
            IsActive = true,
            IsHeadOffice = isHeadOffice,
            AdminUserId = adminUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void UpdateContactInfo(string email, string phone)
    {
        ContactEmail = email;
        ContactPhone = phone;
    }

    public void SetAdmin(Guid adminUserId, Guid changedBy)
    {
        AdminUserId = adminUserId;
    }

    public void RemoveAdmin(Guid removedBy)
    {
        AdminUserId = null;
    }

    // Access control methods expected by Business entity
    public bool IsAdminUser(Guid userId) => AdminUserId.HasValue && AdminUserId.Value == userId;
    public bool IsUserInBranch(Guid userId) => _users.Any(u => u.Id == userId);
}