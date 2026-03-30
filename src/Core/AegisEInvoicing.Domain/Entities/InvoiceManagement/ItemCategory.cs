using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.BusinessManagement;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

public class ItemCategory : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public Guid BusinessID { get; private set; }


    // Navigation properties
    public Business Business { get; private set; } = null!;

    // Many-to-many relationship with BusinessItem
    private readonly List<BusinessItemItemCategory> _businessItems = [];
    public IReadOnlyCollection<BusinessItemItemCategory> BusinessItems => _businessItems.AsReadOnly();

    // Computed property for easier access to items
    public IEnumerable<BusinessItem> Items => _businessItems.Select(bi => bi.BusinessItem);

    // Private constructor for EF Core
    private ItemCategory() { }

    // Private constructor for factory methods
    private ItemCategory(
        string name,
        string description,
        Guid businessId)
    {
        Name = name;
        Description = description;
        BusinessID = businessId;
    }

    #region Factory Methods

    /// <summary>
    /// Creates a new ItemCategory with all required information
    /// </summary>
    public static ItemCategory Create(
        string name,
        string description,
        Guid businessId)
    {
        ValidateRequiredFields(name, description, businessId);

        var category = new ItemCategory(name, description, businessId);

        // Add domain event if needed
        // category.AddDomainEvent(new ItemCategoryCreatedDomainEvent(category.Id, category.Name, businessId));

        return category;
    }

    /// <summary>
    /// Creates a new ItemCategory with Business entity reference
    /// </summary>
    public static ItemCategory CreateWithBusiness(
        string name,
        string description,
        Business business)
    {
        if (business == null)
            throw new ArgumentNullException(nameof(business));

        var category = Create(name, description, business.Id);
        category.Business = business;

        return category;
    }

    #endregion

    #region Domain Methods

    /// <summary>
    /// Updates the category's name
    /// </summary>
    public void UpdateName(string? newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Category name cannot be empty", nameof(newName));

        if (newName.Length > 100) // Assuming max length
            throw new ArgumentException("Category name cannot exceed 100 characters", nameof(newName));

        if (Name.Equals(newName, StringComparison.OrdinalIgnoreCase))
            return; // No change needed

        var oldName = Name;
        Name = newName.Trim();

        // Add domain event
        // AddDomainEvent(new ItemCategoryNameChangedDomainEvent(Id, oldName, Name));
    }

    /// <summary>
    /// Updates the category's description
    /// </summary>
    public void UpdateDescription(string? newDescription)
    {
        // Description can be empty/null
        newDescription = newDescription?.Trim() ?? string.Empty;

        if (newDescription.Length > 500) // Assuming max length
            throw new ArgumentException("Category description cannot exceed 500 characters", nameof(newDescription));

        if (Description.Equals(newDescription, StringComparison.Ordinal))
            return; // No change needed

        var oldDescription = Description;
        Description = newDescription;

        // Add domain event
        // AddDomainEvent(new ItemCategoryDescriptionChangedDomainEvent(Id, oldDescription, Description));
    }

    /// <summary>
    /// Updates both name and description in a single operation
    /// </summary>
    public void UpdateDetails(string newName, string newDescription)
    {
        UpdateName(newName);
        UpdateDescription(newDescription);
    }

    /// <summary>
    /// Associates the category with a different business
    /// </summary>
    public void TransferToBusiness(Business newBusiness)
    {
        if (newBusiness == null)
            throw new ArgumentNullException(nameof(newBusiness));

        if (BusinessID == newBusiness.Id)
            return; // Already belongs to this business

        var oldBusinessId = BusinessID;
        BusinessID = newBusiness.Id;
        Business = newBusiness;

        // Add domain event
        // AddDomainEvent(new ItemCategoryBusinessTransferredDomainEvent(Id, oldBusinessId, newBusiness.Id));
    }

    /// <summary>
    /// Checks if the category belongs to the specified business
    /// </summary>
    public bool BelongsToBusiness(Guid businessId)
    {
        return BusinessID == businessId;
    }

    /// <summary>
    /// Checks if the category name matches (case-insensitive)
    /// </summary>
    public bool HasName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) &&
               Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the category has a meaningful description
    /// </summary>
    public bool HasDescription()
    {
        return !string.IsNullOrWhiteSpace(Description);
    }

    /// <summary>
    /// Gets the category's display name with business context
    /// </summary>
    public string GetDisplayName()
    {
        return Business != null ? $"{Name} ({Business.Name})" : Name;
    }

    /// <summary>
    /// Gets a formatted description for display purposes
    /// </summary>
    public string GetFormattedDescription()
    {
        return HasDescription() ? Description : "No description available";
    }

    /// <summary>
    /// Validates if the category is ready for use (has all necessary information)
    /// </summary>
    public bool IsComplete()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               BusinessID != Guid.Empty;
    }

    /// <summary>
    /// Checks if the category can be deleted (business rules)
    /// </summary>
    public bool CanBeDeleted()
    {
        // Add business logic here - e.g., check if category has items assigned,
        // is referenced in transactions, etc.
        // For now, basic implementation
        return true;
    }

    /// <summary>
    /// Checks if the category can be archived instead of deleted
    /// </summary>
    public bool CanBeArchived()
    {
        // Business rule: categories with items should be archived, not deleted
        return true; // Implement based on your business needs
    }

    /// <summary>
    /// Archives the category (soft delete with business logic)
    /// </summary>
    public void Archive()
    {
        if (!CanBeArchived())
            throw new InvalidOperationException("Category cannot be archived at this time");

        // Add archived flag or implement your archiving logic
        // IsArchived = true;
        // ArchivedOn = DateTime.UtcNow;

        // Add domain event
        // AddDomainEvent(new ItemCategoryArchivedDomainEvent(Id, Name));
    }

    /// <summary>
    /// Compares categories for sorting purposes
    /// </summary>
    public int CompareTo(ItemCategory? other)
    {
        if (other is null) return 1;
        return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if two categories have similar names (for duplicate detection)
    /// </summary>
    public bool IsSimilarTo(ItemCategory? other)
    {
        if (other is null) return false;
        if (BusinessID != other.BusinessID) return false;

        // Simple similarity check - can be enhanced with fuzzy matching
        var thisName = Name.ToLowerInvariant().Replace(" ", "");
        var otherName = other.Name.ToLowerInvariant().Replace(" ", "");

        return thisName == otherName;
    }

    #endregion

    #region Private Helper Methods

    private static void ValidateRequiredFields(
        string name,
        string? description,
        Guid businessId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Category name cannot exceed 100 characters", nameof(name));

        if (description != null && description.Length > 500)
            throw new ArgumentException("Category description cannot exceed 500 characters", nameof(description));

        if (businessId == Guid.Empty)
            throw new ArgumentException("Business ID cannot be empty", nameof(businessId));
    }

    #endregion
}