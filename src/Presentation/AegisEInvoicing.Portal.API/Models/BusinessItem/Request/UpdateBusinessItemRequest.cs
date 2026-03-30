using System.ComponentModel.DataAnnotations;

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
    /// Service code information
    /// </summary>
    [Required(ErrorMessage = "Service code is required")]
    public ServiceCodeRequest ServiceCode { get; set; } = null!;

    /// <summary>
    /// Tax category information
    /// </summary>
    [Required(ErrorMessage = "Tax category is required")]
    public TaxCategoryRequest TaxCategory { get; set; } = null!;

    /// <summary>
    /// Item category ID
    /// </summary>
    [Required(ErrorMessage = "Item category is required")]
    public Guid ItemCategoryId { get; set; }

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
}