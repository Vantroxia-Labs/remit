using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.ERP.API.Models;

/// <summary>
/// Represents an individual item in an invoice
/// </summary>
public class CreateInvoiceItemDto
{
    /// <summary>
    /// Name of the item or service
    /// </summary>
    /// <example>Electrical Cable (100m Roll)</example>
    [Required(ErrorMessage = "Item name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Item name must be between 1 and 200 characters")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Detailed description of the item
    /// </summary>
    /// <example>High-voltage copper electrical cable suitable for industrial power installations</example>
    [Required(ErrorMessage = "Item description is required")]
    [StringLength(1000, ErrorMessage = "Item description cannot exceed 1000 characters")]
    public string ItemDescription { get; set; } = null!;

    /// <summary>
    /// Item category (e.g., Goods, Service, Equipment)
    /// </summary>
    /// <example>Electrical Materials</example>


    /// <summary>
    /// Service code information (mapped to FIRS service catalog)
    /// </summary>
    /// <example>
    /// {
    ///   "code": "E1234",
    ///   "name": "Electrical Installation Service"
    /// }
    /// </example>
    [Required(ErrorMessage = "Service code is required")]
    public ServiceCodeRequest ServiceCode { get; set; } = null!;

    /// <summary>
    /// Tax categories applied to this item
    /// </summary>
    public List<TaxCategoryRequest> TaxCategories { get; set; } = [];

    /// <summary>
    /// Unit price of the item (in NGN)
    /// </summary>
    /// <example>2500.00</example>
    [Required(ErrorMessage = "Unit price is required")]
    [Range(1, double.MaxValue, ErrorMessage = "Unit price must be greater than or equal to 1")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Quantity of the item purchased
    /// </summary>
    /// <example>100</example>
    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, double.MaxValue, ErrorMessage = "Quantity must be greater than or equal to 1")]
    public decimal Quantity { get; init; }

    /// <summary>
    /// Discount fee details (optional)
    /// </summary>
    /// <example>
    /// {
    ///   "amount": 10,
    ///   "code": "Percent"
    /// }
    /// </example>
    public DiscountFeeDto? DiscountFee { get; init; }

    /// <summary>
    /// Additional fee details (optional)
    /// </summary>
    /// <example>
    /// {
    ///   "amount": 2000,
    ///   "code": "NGN"
    /// }
    /// </example>
    public AdditionalFeeDto? AdditionalFee { get; init; }
}


/// <summary>
/// Service code request model
/// </summary>
public class ServiceCodeRequest
{
    /// <summary>
    /// Service code identifier
    /// </summary>
    /// <example>E1234</example>
    [Required(ErrorMessage = "Service code is required")]
    [StringLength(50, ErrorMessage = "Service code cannot exceed 50 characters")]
    public string Code { get; set; } = null!;

    /// <summary>
    /// Descriptive name of the service or product
    /// </summary>
    /// <example>Electrical Installation Service</example>
    [Required(ErrorMessage = "Service code name is required")]
    [StringLength(200, ErrorMessage = "Service code name cannot exceed 200 characters")]
    public string Name { get; set; } = null!;
}

/// <summary>
/// Tax category request model
/// </summary>
public class TaxCategoryRequest
{
    /// <summary>
    /// Name of the tax category (e.g., VAT, WHT, Zero-Rated)
    /// </summary>
    [Required(ErrorMessage = "Tax category name is required")]
    [StringLength(500, ErrorMessage = "Tax category name cannot exceed 500 characters")]
    public string Name { get; set; } = null!;

    /// <summary>True = percentage-based; False = flat fee</summary>
    public bool IsPercentage { get; set; }

    /// <summary>Rate (0-100) when IsPercentage is true</summary>
    [Range(0, 100, ErrorMessage = "Tax percentage must be between 0 and 100")]
    public decimal? Percent { get; set; }

    /// <summary>Fixed amount when IsPercentage is false</summary>
    public decimal? FlatAmount { get; set; }
}
