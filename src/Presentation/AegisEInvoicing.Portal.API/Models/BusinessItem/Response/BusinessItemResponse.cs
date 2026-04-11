using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Portal.API.Models.BusinessItem.Response;

/// <summary>
/// Response model for business item operations
/// </summary>
public class BusinessItemResponse
{
    /// <summary>
    /// Unique identifier of the business item
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// System-generated item ID
    /// </summary>
    public string ItemId { get; set; } = null!;

    /// <summary>
    /// Name of the item
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Item type: Goods or Service
    /// </summary>
    public ItemType ItemType { get; set; }

    /// <summary>
    /// Product code (Goods) or Service code (Services)
    /// </summary>
    public ServiceCodeResponse ServiceCode { get; set; } = null!;

    /// <summary>
    /// Item category ID
    /// </summary>
    public Guid ItemCategoryId { get; set; }

    /// <summary>
    /// Item category name
    /// </summary>
    public string? ItemCategoryName { get; set; }

    /// <summary>
    /// Description of the item
    /// </summary>
    public string ItemDescription { get; set; } = null!;

    /// <summary>
    /// Unit price of the item
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Business ID this item belongs to
    /// </summary>
    public Guid BusinessId { get; set; }

    /// <summary>
    /// Business name
    /// </summary>
    public string? BusinessName { get; set; }

    /// <summary>
    /// Date and time when the item was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Date and time when the item was last updated
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// User ID who created the item
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// User ID who last updated the item
    /// </summary>
    public Guid? UpdatedBy { get; set; }
}

/// <summary>
/// Service/Product code response model
/// </summary>
public class ServiceCodeResponse
{
    /// <summary>
    /// Code value
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// Code description
    /// </summary>
    public string Name { get; set; } = null!;
}

/// <summary>
/// Summary response for business item list
/// </summary>
public class BusinessItemSummaryResponse
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// System-generated item ID
    /// </summary>
    public string ItemId { get; set; } = null!;

    /// <summary>
    /// Name of the item
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Item type: Goods or Service
    /// </summary>
    public ItemType ItemType { get; set; }

    /// <summary>
    /// Code value
    /// </summary>
    public string ServiceCodeName { get; set; } = null!;

    /// <summary>
    /// Item category name
    /// </summary>
    public string ItemCategoryName { get; set; } = null!;

    /// <summary>
    /// Unit price
    /// </summary>
    public double UnitPrice { get; set; }

    /// <summary>
    /// Business name
    /// </summary>
    public string BusinessName { get; set; } = null!;

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Response for successful business item creation
/// </summary>
public class CreateBusinessItemResponse
{
    /// <summary>
    /// ID of the created business item
    /// </summary>
    public Guid BusinessItemId { get; set; }

    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = null!;
}

/// <summary>
/// Response for successful business item update
/// </summary>
public class UpdateBusinessItemResponse
{
    /// <summary>
    /// ID of the updated business item
    /// </summary>
    public Guid BusinessItemId { get; set; }

    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = null!;
}
