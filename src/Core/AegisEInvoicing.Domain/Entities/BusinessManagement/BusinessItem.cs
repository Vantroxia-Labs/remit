using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Domain.Entities.BusinessManagement;

public class BusinessItem : AuditableAggregateRoot
{
    public string ItemId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public ItemType ItemType { get; private set; }
    public ServiceCode ServiceCode { get; private set; } = null!;

    public string ItemDescription { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public Guid BusinessID { get; private set; }

    // Navigation property
    public Business Business { get; private set; } = null!;

    // Collections
    private readonly List<InvoiceItem> _invoiceItems = [];
    public IReadOnlyCollection<InvoiceItem> InvoiceItems => _invoiceItems.AsReadOnly();

    private readonly List<BusinessItemPriceHistory> _priceHistory = [];
    public IReadOnlyCollection<BusinessItemPriceHistory> PriceHistory => _priceHistory.AsReadOnly();

    private readonly List<BusinessItemTaxCategory> _taxCategories = [];
    public IReadOnlyCollection<BusinessItemTaxCategory> TaxCategories => _taxCategories.AsReadOnly();

    private BusinessItem() { } // Required for EF Core

    /// <summary>
    /// Factory method for creating a new BusinessItem.
    /// </summary>
    public static BusinessItem Create(
        Guid businessId,
        string name,
        ItemType itemType,
        ServiceCode serviceCode,
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

        return new BusinessItem
        {
            ItemId = new ItemId().FullId,
            BusinessID = businessId,
            Name = name,
            ItemType = itemType,
            ServiceCode = serviceCode,
            ItemDescription = itemDescription,
            UnitPrice = unitPrice
        };
    }

    /// <summary>
    /// Update non-price properties. Price changes require approval via ProposePrice.
    /// </summary>
    public void Update(
        string name,
        ItemType itemType,
        ServiceCode serviceCode,
        Guid itemCategoryId,
        string itemDescription)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        if (serviceCode is null)
            throw new ArgumentException("ServiceCode cannot be empty.", nameof(serviceCode));

        Name = name;
        ItemType = itemType;
        ServiceCode = serviceCode;
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
    /// Replaces the entire tax category collection.
    /// Pass an empty enumerable to clear all tax categories.
    /// </summary>
    public void UpdateTaxCategories(IEnumerable<BusinessItemTaxCategory> taxCategories)
    {
        _taxCategories.Clear();
        _taxCategories.AddRange(taxCategories);
    }
}
