using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Events.UserManagement;

namespace AegisEInvoicing.Domain.Entities.UserManagement;

/// <summary>
/// Represents a platform-wide role definition managed exclusively by Aegis
/// These roles serve as templates that merchants and branches can assign to their users
/// Only platform administrators can create, modify, or delete role definitions
/// </summary>
public class PlatformRole : AuditableAggregateRoot
{
    private readonly List<string> _permissions = [];

    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Category { get; private set; } // e.g., "Administrative", "Operational", "Viewer"
    public bool IsActive { get; private set; }
    public bool IsSystemRole { get; private set; }
    public int SortOrder { get; private set; }
    /// <summary>
    /// Null = platform-wide system role (managed by Aegis only).
    /// Non-null = custom role created by that business's ClientAdmin.
    /// </summary>
    public Guid? BusinessId { get; private set; }

    public IReadOnlyCollection<string> Permissions => _permissions.AsReadOnly();

    private PlatformRole(
        string name,
        string description,
        string category,
        int sortOrder,
        bool isSystemRole = false,
        Guid? businessId = null)
    {
        Name = name;
        Description = description;
        Category = category;
        SortOrder = sortOrder;
        IsActive = true;
        IsSystemRole = isSystemRole;
        BusinessId = businessId;
    }

    public static PlatformRole Create(
        string name,
        string description,
        string category,
        int sortOrder,
        Guid createdBy,
        bool isSystemRole = false)
    {
        ValidateInputs(name, description, category);

        var role = new PlatformRole(name, description, category, sortOrder, isSystemRole);

        role.AddDomainEvent(new PlatformRoleCreatedEvent(
            role.Id,
            role.Name,
            role.Description,
            role.Category,
            createdBy,
            DateTimeOffset.UtcNow));

        return role;
    }

    /// <summary>
    /// Creates a custom role scoped to a specific business.
    /// ClientAdmins call this to build their own permission sets.
    /// Only permissions from PermissionConstants.ClientAdminAssignablePermissions are allowed.
    /// </summary>
    public static PlatformRole CreateBusinessRole(
        string name,
        string description,
        Guid businessId,
        Guid createdBy,
        IEnumerable<string>? permissions = null)
    {
        ValidateInputs(name, description, category: "Custom");

        var role = new PlatformRole(name, description, category: "Custom", sortOrder: 99, isSystemRole: false, businessId: businessId);

        if (permissions != null)
        {
            foreach (var p in permissions)
                role.AddPermission(p);
        }

        role.AddDomainEvent(new PlatformRoleCreatedEvent(
            role.Id,
            role.Name,
            role.Description,
            role.Category,
            createdBy,
            DateTimeOffset.UtcNow));

        return role;
    }

    public static PlatformRole CreateSystemRole(
        string name,
        string description,
        string category,
        int sortOrder,
        Guid createdBy,
        IEnumerable<string> permissions)
    {
        var role = Create(name, description, category, sortOrder, createdBy, isSystemRole: true);

        foreach (var permission in permissions)
        {
            role.AddPermission(permission);
        }

        return role;
    }

    public void Update(
        string description,
        string category,
        int sortOrder,
        Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category is required", nameof(category));

        var changes = new Dictionary<string, object>();
        if (Description != description) changes["Description"] = description;
        if (Category != category) changes["Category"] = category;
        if (SortOrder != sortOrder) changes["SortOrder"] = sortOrder;

        Description = description;
        Category = category;
        SortOrder = sortOrder;

        if (changes.Count > 0)
        {
            AddDomainEvent(new PlatformRoleUpdatedEvent(Id, Name, updatedBy, changes, DateTimeOffset.UtcNow));
        }
    }

    public void Activate(Guid activatedBy)
    {
        if (IsActive)
            throw new InvalidOperationException("Role is already active");

        IsActive = true;
        AddDomainEvent(new PlatformRoleActivatedEvent(Id, Name, activatedBy, DateTimeOffset.UtcNow));
    }

    public void Deactivate(Guid deactivatedBy)
    {
        if (!IsActive)
            throw new InvalidOperationException("Role is already inactive");

        if (IsSystemRole)
            throw new InvalidOperationException("System roles cannot be deactivated");

        IsActive = false;
        AddDomainEvent(new PlatformRoleDeactivatedEvent(Id, Name, deactivatedBy, DateTimeOffset.UtcNow));
    }

    public void AddPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission cannot be empty", nameof(permission));

        if (_permissions.Contains(permission))
            return;

        _permissions.Add(permission);
        AddDomainEvent(new PlatformRolePermissionAddedEvent(Id, Name, permission, DateTimeOffset.UtcNow));
    }

    public void RemovePermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission cannot be empty", nameof(permission));

        if (!_permissions.Contains(permission))
            return;

        _permissions.Remove(permission);
        AddDomainEvent(new PlatformRolePermissionRemovedEvent(Id, Name, permission, DateTimeOffset.UtcNow));
    }

    public bool HasPermission(string permission) => _permissions.Contains(permission);

    private static void ValidateInputs(string name, string description, string category)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Role description is required", nameof(description));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Role category is required", nameof(category));

        if (name.Length > 50)
            throw new ArgumentException("Role name cannot exceed 50 characters", nameof(name));

        if (description.Length > 200)
            throw new ArgumentException("Role description cannot exceed 200 characters", nameof(description));

        if (category.Length > 30)
            throw new ArgumentException("Role category cannot exceed 30 characters", nameof(category));
    }
}