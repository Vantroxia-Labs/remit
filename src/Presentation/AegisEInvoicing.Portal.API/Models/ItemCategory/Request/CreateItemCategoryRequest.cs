using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.ItemCategory.Request;

/// <summary>
/// Request model for creating a new item category
/// </summary>
public class CreateItemCategoryRequest
{
    /// <summary>
    /// Name of the item category
    /// </summary>
    [Required(ErrorMessage = "Item category name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the item category
    /// </summary>
    [Required(ErrorMessage = "Item category description is required")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 500 characters")]
    public string Description { get; set; } = string.Empty;
}