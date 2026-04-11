using System.ComponentModel.DataAnnotations;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Portal.API.Models.BusinessItem.Request;

/// <summary>
/// Request model for creating a new business item
/// </summary>
public class CreateBusinessItemRequest
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

    /// <summary>
    /// Tax categories applicable to this item
    /// </summary>
    public List<TaxCategoryItemRequest> TaxCategories { get; set; } = [];
}

/// <summary>
/// Service/Product code request model
/// </summary>
public class ServiceCodeRequest
{
    /// <summary>
    /// Code value
    /// </summary>
    [Required(ErrorMessage = "Code is required")]
    [StringLength(50, ErrorMessage = "Code cannot exceed 50 characters")]
    public string Code { get; set; } = null!;

    /// <summary>
    /// Code description
    /// </summary>
    [Required(ErrorMessage = "Code description is required")]
    [StringLength(200, ErrorMessage = "Code description cannot exceed 200 characters")]
    public string Name { get; set; } = null!;
}

/// <summary>
/// Tax category request model
/// </summary>
public class TaxCategoryItemRequest
{
    /// <summary>FIRS tax category code</summary>
    [Required(ErrorMessage = "Tax category code is required")]
    [StringLength(50, ErrorMessage = "Code cannot exceed 50 characters")]
    public string Code { get; set; } = null!;

    /// <summary>Display name</summary>
    [Required(ErrorMessage = "Tax category name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = null!;

    /// <summary>True = percentage-based; False = flat fee</summary>
    public bool IsPercentage { get; set; }

    /// <summary>Rate (0–100) when IsPercentage is true</summary>
    public decimal? Percent { get; set; }

    /// <summary>Fixed amount when IsPercentage is false</summary>
    public decimal? FlatAmount { get; set; }
}