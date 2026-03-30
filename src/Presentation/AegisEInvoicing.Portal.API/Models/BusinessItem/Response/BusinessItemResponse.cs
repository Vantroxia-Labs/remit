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
    /// Service code information
    /// </summary>
    public ServiceCodeResponse ServiceCode { get; set; } = null!;

    /// <summary>
    /// Tax category information
    /// </summary>
    public TaxCategoryResponse TaxCategory { get; set; } = null!;

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
/// Service code response model
/// </summary>
public class ServiceCodeResponse
{
    /// <summary>
    /// Service code
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// Service code name
    /// </summary>
    public string Name { get; set; } = null!;
}

/// <summary>
/// Tax category response model
/// </summary>
public class TaxCategoryResponse
{
    /// <summary>
    /// Tax category name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Tax percentage
    /// </summary>
    public decimal Percent { get; set; }
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
    /// Service code name
    /// </summary>
    public string ServiceCodeName { get; set; } = null!;

    /// <summary>
    /// Tax category name
    /// </summary>
    public string TaxCategoryName { get; set; } = null!;

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