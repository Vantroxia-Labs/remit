using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;

namespace AegisEInvoicing.Domain.Entities.BusinessManagement;

/// <summary>
/// Junction entity representing the many-to-many relationship between BusinessItem and ItemCategory
/// Allows a business item to belong to multiple categories
/// </summary>
public class BusinessItemItemCategory : AuditableEntity
{
    public Guid BusinessItemId { get; private set; }
    public Guid ItemCategoryId { get; private set; }

    // Navigation properties
    public BusinessItem BusinessItem { get; private set; } = null!;
    public ItemCategory ItemCategory { get; private set; } = null!;

    private BusinessItemItemCategory() { } // EF Core constructor

    public static BusinessItemItemCategory Create(Guid businessItemId, Guid itemCategoryId)
    {
        if (businessItemId == Guid.Empty)
            throw new ArgumentException("Business item ID cannot be empty", nameof(businessItemId));

        if (itemCategoryId == Guid.Empty)
            throw new ArgumentException("Item category ID cannot be empty", nameof(itemCategoryId));

        return new BusinessItemItemCategory
        {
            BusinessItemId = businessItemId,
            ItemCategoryId = itemCategoryId
        };
    }
}
