using System.ComponentModel.DataAnnotations;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Portal.API.Models.BusinessItem.Request;

/// <summary>
/// Request model for updating an existing business item
/// </summary>
public class UpdateBusinessItemRequest
{
    /// <summary>
    /// Name of the item
    /// </summary>
    [Required(ErrorMessage = "Item name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Item name must be between 1 and 200 characters")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Item type: Goods or Service
    /// </summary>
    [Required(ErrorMessage = "Item type is required")]
    public ItemType ItemType { get; set; }

    /// <summary>
    /// Product code (for Goods) or Service code (for Services)
    /// </summary>
    [Required(ErrorMessage = "Code is required")]
    public ServiceCodeRequest ServiceCode { get; set; } = null!;

    /// <summary>
    /// Description of the item
    /// </summary>
    [Required(ErrorMessage = "Item description is required")]
    [StringLength(1000, ErrorMessage = "Item description cannot exceed 1000 characters")]
    public string ItemDescription { get; set; } = null!;

    /// <summary>
    /// Unit price of the item
    /// </summary>
    [Required(ErrorMessage = "Unit price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be greater than or equal to 0")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Tax categories applicable to this item
    /// </summary>
    public List<TaxCategoryItemRequest> TaxCategories { get; set; } = [];
}