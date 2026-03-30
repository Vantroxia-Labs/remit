namespace AegisEInvoicing.Portal.API.Models.ItemCategory.Response;

/// <summary>
/// Response model for item category operations
/// </summary>
public class ItemCategoryResponse
{
    /// <summary>
    /// Unique identifier for the item category
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the item category
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the item category
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the item category was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Response model for item category creation
/// </summary>
public class CreateItemCategoryResponse
{
    /// <summary>
    /// ID of the newly created item category
    /// </summary>
    public Guid ItemCategoryId { get; set; }

    /// <summary>
    /// Success or error message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response model for item category update
/// </summary>
public class UpdateItemCategoryResponse
{
    /// <summary>
    /// ID of the updated item category
    /// </summary>
    public Guid ItemCategoryId { get; set; }

    /// <summary>
    /// Success or error message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}