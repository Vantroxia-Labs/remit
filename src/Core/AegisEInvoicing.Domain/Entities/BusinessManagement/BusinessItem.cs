using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;

namespace AegisEInvoicing.Domain.Entities.BusinessManagement;

public class BusinessItem : AuditableAggregateRoot
{
    public string ItemId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public ServiceCode ServiceCode { get; private set; } = null!;
    public TaxCategory TaxCategory { get; private set; } = null!;
    
    // Primary category (kept for backward compatibility - this is the "main" category)
    public Guid ItemCategoryId { get; private set; }
    
    public string ItemDescription { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public Guid BusinessID { get; private set; }

    // Navigation property
    public Business Business { get; private set; } = null!;
    
    // Primary category navigation (for backward compatibility)
    public ItemCategory ItemCategory { get; private set; } = null!;

    // Many-to-many relationship with ItemCategory (new feature)
    private readonly List<BusinessItemItemCategory> _itemCategories = [];
    public IReadOnlyCollection<BusinessItemItemCategory> ItemCategories => _itemCategories.AsReadOnly();

    // Computed property for easier access to all categories
    public IEnumerable<ItemCategory> Categories => _itemCategories.Select(ic => ic.ItemCategory);

    // Collections
    private readonly List<InvoiceItem> _invoiceItems = [];
    public IReadOnlyCollection<InvoiceItem> InvoiceItems => _invoiceItems.AsReadOnly();

    private readonly List<BusinessItemPriceHistory> _priceHistory = [];
    public IReadOnlyCollection<BusinessItemPriceHistory> PriceHistory => _priceHistory.AsReadOnly();

    private BusinessItem() { } // Required for EF Core

    /// <summary>
    /// Factory method for creating a new BusinessItem with a single category.
    /// </summary>
    public static BusinessItem Create(
        Guid businessId,
        string name,
        ServiceCode serviceCode,
        TaxCategory taxCategory,
        Guid itemCategoryId,
        string itemDescription,
        decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        if (serviceCode is null)
            throw new ArgumentException("ServiceCode cannot be empty.", nameof(serviceCode));

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "UnitPrice cannot be negative.");

        var item = new BusinessItem
        {
            ItemId = new ItemId().FullId,
            BusinessID = businessId,
            Name = name,
            ServiceCode = serviceCode,
            TaxCategory = taxCategory,
            ItemCategoryId = itemCategoryId,
            ItemDescription = itemDescription,
            UnitPrice = unitPrice
        };
        
        // Also add to the many-to-many collection
        var itemCategory = BusinessItemItemCategory.Create(item.Id, itemCategoryId);
        item._itemCategories.Add(itemCategory);
        
        return item;
    }

    /// <summary>
    /// Update non-price properties. Price changes require approval via ProposePrice.
    /// </summary>
    public void Update(
        string name,
        ServiceCode serviceCode,
        TaxCategory taxCategory,
        Guid itemCategoryId,
        string itemDescription)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        if (serviceCode is null)
            throw new ArgumentException("ServiceCode cannot be empty.", nameof(serviceCode));

        Name = name;
        ServiceCode = serviceCode;
        TaxCategory = taxCategory;
        ItemCategoryId = itemCategoryId;
        ItemDescription = itemDescription;
    }

    /// <summary>
    /// Proposes a price change that requires ClientAdmin approval.
    /// Returns the created price history record.
    /// </summary>
    public BusinessItemPriceHistory ProposePrice(decimal newPrice, string? comments = null)
    {
        if (newPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice), "New price cannot be negative.");

        if (newPrice == UnitPrice)
            throw new ArgumentException("New price must be different from current price.", nameof(newPrice));

        var priceHistory = BusinessItemPriceHistory.Create(Id, UnitPrice, newPrice, comments);
        _priceHistory.Add(priceHistory);
        return priceHistory;
    }

    /// <summary>
    /// Applies an approved price change. Called after ClientAdmin approval.
    /// </summary>
    public void ApplyApprovedPrice(decimal approvedPrice)
    {
        if (approvedPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(approvedPrice), "Price cannot be negative.");

        UnitPrice = approvedPrice;
    }

    /// <summary>
    /// Directly updates the price without approval workflow.
    /// Use for ERP integrations where approval has already occurred externally.
    /// </summary>
    public void UpdatePriceFromErp(decimal unitPrice)
    {
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "UnitPrice cannot be negative.");

        UnitPrice = unitPrice;
    }

    /// <summary>
    /// Checks if there are pending price changes awaiting approval.
    /// </summary>
    public bool HasPendingPriceChange => _priceHistory.Any(ph => ph.Status == Enums.ApprovalStatus.Pending);

    /// <summary>
    /// Update the description of the item.
    /// </summary>
    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty.", nameof(description));

        ItemDescription = description;
    }

    /// <summary>
    /// Update the primary category of the item.
    /// Also ensures the category is in the many-to-many collection.
    /// </summary>
    public void UpdateCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category cannot be empty.", nameof(categoryId));

        ItemCategoryId = categoryId;
        
        // Ensure the category is also in the many-to-many collection
        if (!_itemCategories.Any(ic => ic.ItemCategoryId == categoryId))
        {
            var itemCategory = BusinessItemItemCategory.Create(Id, categoryId);
            _itemCategories.Add(itemCategory);
        }
    }

    /// <summary>
    /// Adds a category to this business item (many-to-many relationship).
    /// </summary>
    public void AddCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category ID cannot be empty", nameof(categoryId));

        // Check if category is already associated
        if (_itemCategories.Any(ic => ic.ItemCategoryId == categoryId))
            return; // Already exists, no need to add

        var itemCategory = BusinessItemItemCategory.Create(Id, categoryId);
        _itemCategories.Add(itemCategory);
    }

    /// <summary>
    /// Removes a category from this business item.
    /// Cannot remove the primary category.
    /// </summary>
    public void RemoveCategory(Guid categoryId)
    {
        if (categoryId == ItemCategoryId)
            throw new InvalidOperationException("Cannot remove the primary category. Update the primary category first.");

        var itemCategory = _itemCategories.FirstOrDefault(ic => ic.ItemCategoryId == categoryId);
        if (itemCategory != null)
        {
            _itemCategories.Remove(itemCategory);
        }
    }

    /// <summary>
    /// Checks if this item belongs to a specific category.
    /// </summary>
    public bool BelongsToCategory(Guid categoryId)
    {
        return _itemCategories.Any(ic => ic.ItemCategoryId == categoryId);
    }

    /// <summary>
    /// Gets all category IDs this item belongs to.
    /// </summary>
    public IEnumerable<Guid> GetCategoryIds()
    {
        return _itemCategories.Select(ic => ic.ItemCategoryId);
    }
}