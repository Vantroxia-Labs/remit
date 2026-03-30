using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Responses.GetPurchaseInvoices;

/// <summary>
/// Response from GetPurchaseInvoices endpoint containing received invoices for a taxpayer
/// </summary>
public sealed class GetPurchaseInvoicesResponse
{
    /// <summary>
    /// TIN
    /// </summary>
    [JsonPropertyName("tin")]
    public string Tin { get; set; } = null!;

    /// <summary>
    /// Start Date
    /// </summary>
    [JsonPropertyName("start_date")]
    public string StartDate { get; set; } = null!;

    /// <summary>
    /// End Date
    /// </summary>
    [JsonPropertyName("end_date")]
    public string EndDate { get; set; } = null!;

    /// <summary>
    /// Count
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// data - Array of purchase invoice items
    /// </summary>
    [JsonPropertyName("data")]
    public List<PurchaseInvoiceItem> Data { get; set; } = new();
}

/// <summary>
/// Purchase invoices data with pagination information
/// </summary>
public sealed class GetPurchaseInvoicesData
{
    /// <summary>
    /// List of purchase invoice items
    /// </summary>
    [JsonPropertyName("items")]
    public List<PurchaseInvoiceItem> Items { get; set; } = new();

    /// <summary>
    /// Pagination information
    /// </summary>
    [JsonPropertyName("page")]
    public PurchaseInvoicePage Page { get; set; } = new();

    /// <summary>
    /// Additional attributes (if any)
    /// </summary>
    [JsonPropertyName("attributes")]
    public object? Attributes { get; set; }
}

/// <summary>
/// Individual purchase invoice item with complete invoice details
/// </summary>
public sealed class PurchaseInvoiceItem
{
    /// <summary>
    /// Business ID from the invoice
    /// </summary>
    [JsonPropertyName("business_id")]
    public string? BusinessId { get; set; }

    /// <summary>
    /// Invoice Reference Number (IRN) - Unique identifier
    /// </summary>
    [JsonPropertyName("irn")]
    public string IRN { get; set; } = string.Empty;

    /// <summary>
    /// Customization ID
    /// </summary>
    [JsonPropertyName("customization_id")]
    public string? CustomizationId { get; set; }

    /// <summary>
    /// Invoice type code (e.g., 380 for commercial invoice)
    /// </summary>
    [JsonPropertyName("invoice_type_code")]
    public string InvoiceTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// Date when invoice was issued
    /// </summary>
    [JsonPropertyName("issue_date")]
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Time when invoice was issued
    /// </summary>
    [JsonPropertyName("issue_time")]
    public string IssueTime { get; set; } = string.Empty;

    /// <summary>
    /// Invoice due date for payment
    /// </summary>
    [JsonPropertyName("due_date")]
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Currency code for the invoice (e.g., NGN)
    /// </summary>
    [JsonPropertyName("document_currency_code")]
    public string DocumentCurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Currency code for tax calculations
    /// </summary>
    [JsonPropertyName("tax_currency_code")]
    public string TaxCurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Payment status of the invoice
    /// </summary>
    [JsonPropertyName("payment_status")]
    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>
    /// Entry status in FIRS system
    /// </summary>
    [JsonPropertyName("entry_status")]
    public string EntryStatus { get; set; } = string.Empty;

    /// <summary>
    /// Date when invoice was synced to FIRS
    /// </summary>
    [JsonPropertyName("sync_date")]
    public string? SyncDate { get; set; }

    /// <summary>
    /// Supplier (seller) party information
    /// </summary>
    [JsonPropertyName("accounting_supplier_party")]
    public PurchaseInvoiceParty? SupplierParty { get; set; }

    /// <summary>
    /// Customer (buyer) party information - this is our business receiving the invoice
    /// </summary>
    [JsonPropertyName("accounting_customer_party")]
    public PurchaseInvoiceParty? CustomerParty { get; set; }

    /// <summary>
    /// Total amount before tax
    /// </summary>
    [JsonPropertyName("line_extension_amount")]
    public decimal LineExtensionAmount { get; set; }

    /// <summary>
    /// Total tax amount
    /// </summary>
    [JsonPropertyName("tax_exclusive_amount")]
    public decimal TaxExclusiveAmount { get; set; }

    /// <summary>
    /// Total amount including tax
    /// </summary>
    [JsonPropertyName("tax_inclusive_amount")]
    public decimal TaxInclusiveAmount { get; set; }

    /// <summary>
    /// Total tax amount
    /// </summary>
    [JsonPropertyName("total_tax_amount")]
    public decimal TotalTaxAmount { get; set; }

    /// <summary>
    /// Payable amount (may differ from tax inclusive amount due to rounding)
    /// </summary>
    [JsonPropertyName("payable_amount")]
    public decimal PayableAmount { get; set; }

    /// <summary>
    /// Amount paid
    /// </summary>
    [JsonPropertyName("paid_amount")]
    public decimal? PaidAmount { get; set; }

    /// <summary>
    /// Payable rounding amount
    /// </summary>
    [JsonPropertyName("payable_rounding_amount")]
    public decimal? PayableRoundingAmount { get; set; }

    /// <summary>
    /// Invoice line items
    /// </summary>
    [JsonPropertyName("invoice_line")]
    public List<PurchaseInvoiceLine> InvoiceLines { get; set; } = [];

    /// <summary>
    /// Tax totals breakdown
    /// </summary>
    [JsonPropertyName("tax_total")]
    public List<PurchaseInvoiceTaxTotal> TaxTotal { get; set; } = [];

    /// <summary>
    /// Additional notes or remarks
    /// </summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>
    /// Buyer reference
    /// </summary>
    [JsonPropertyName("buyer_reference")]
    public string? BuyerReference { get; set; }

    /// <summary>
    /// Accounting cost
    /// </summary>
    [JsonPropertyName("accounting_cost")]
    public string? AccountingCost { get; set; }

    /// <summary>
    /// Invoice delivery period (start and end dates)
    /// </summary>
    [JsonPropertyName("invoice_delivery_period")]
    public PurchaseInvoiceDeliveryPeriod? InvoiceDeliveryPeriod { get; set; }

    /// <summary>
    /// Payment means information
    /// </summary>
    [JsonPropertyName("payment_means")]
    public List<PurchaseInvoicePaymentMeans>? PaymentMeans { get; set; }

    /// <summary>
    /// Payment terms note
    /// </summary>
    [JsonPropertyName("payment_terms_note")]
    public string? PaymentTermsNote { get; set; }

    /// <summary>
    /// Legal monetary total information
    /// </summary>
    [JsonPropertyName("legal_monetary_total")]
    public PurchaseInvoiceLegalMonetaryTotal? LegalMonetaryTotal { get; set; }
}

/// <summary>
/// Party information (supplier or customer)
/// </summary>
public sealed class PurchaseInvoiceParty
{
    /// <summary>
    /// Party ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Party name
    /// </summary>
    [JsonPropertyName("party_name")]
    public string PartyName { get; set; } = string.Empty;

    /// <summary>
    /// Tax Identification Number
    /// </summary>
    [JsonPropertyName("tin")]
    public string TIN { get; set; } = string.Empty;

    /// <summary>
    /// Business Registration Number
    /// </summary>
    [JsonPropertyName("brn")]
    public string? BRN { get; set; }

    /// <summary>
    /// Business description
    /// </summary>
    [JsonPropertyName("business_description")]
    public string? BusinessDescription { get; set; }

    /// <summary>
    /// Party email address
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Party phone number
    /// </summary>
    [JsonPropertyName("telephone")]
    public string? Telephone { get; set; }

    /// <summary>
    /// Postal address ID
    /// </summary>
    [JsonPropertyName("postal_address_id")]
    public string? PostalAddressId { get; set; }

    /// <summary>
    /// Physical address
    /// </summary>
    [JsonPropertyName("postal_address")]
    public PurchaseInvoiceAddress? Address { get; set; }
}

/// <summary>
/// Address information
/// </summary>
public sealed class PurchaseInvoiceAddress
{
    /// <summary>
    /// Street name
    /// </summary>
    [JsonPropertyName("street_name")]
    public string? StreetName { get; set; }

    /// <summary>
    /// Additional street information
    /// </summary>
    [JsonPropertyName("additional_street_name")]
    public string? AdditionalStreetName { get; set; }

    /// <summary>
    /// City name
    /// </summary>
    [JsonPropertyName("city_name")]
    public string? CityName { get; set; }

    /// <summary>
    /// Postal zone
    /// </summary>
    [JsonPropertyName("postal_zone")]
    public string? PostalZone { get; set; }

    /// <summary>
    /// Country subentity (state/province) - Not present in actual API response
    /// </summary>
    [JsonPropertyName("country_subentity")]
    public string? CountrySubentity { get; set; }

    /// <summary>
    /// Country code (e.g., NG for Nigeria)
    /// </summary>
    [JsonPropertyName("country")]
    public string? CountryIdentificationCode { get; set; }
}

/// <summary>
/// Invoice line item details
/// </summary>
public sealed class PurchaseInvoiceLine
{
    /// <summary>
    /// Quantity
    /// </summary>
    [JsonPropertyName("invoiced_quantity")]
    public int InvoicedQuantity { get; set; }

    /// <summary>
    /// Line extension amount (quantity × price)
    /// </summary>
    [JsonPropertyName("line_extension_amount")]
    public int LineExtensionAmount { get; set; }

    /// <summary>
    /// Item details
    /// </summary>
    [JsonPropertyName("item")]
    public Item Item { get; set; } = null!;

    /// <summary>
    /// Price details
    /// </summary>
    [JsonPropertyName("price")]
    public Price Price { get; set; } = null!;

    /// <summary>
    /// HSN Code
    /// </summary>
    [JsonPropertyName("hsn_code")]
    public string HsnCode { get; set; } = string.Empty;

    /// <summary>
    /// Product Category
    /// </summary>
    [JsonPropertyName("product_category")]
    public string ProductCategory { get; set; } = string.Empty;

    /// <summary>
    /// ISIC Code
    /// </summary>
    [JsonPropertyName("isic_code")]
    public string IsicCode { get; set; } = string.Empty;

    /// <summary>
    /// Service Category
    /// </summary>
    [JsonPropertyName("service_category")]
    public string ServiceCategory { get; set; } = string.Empty;

    [JsonPropertyName("discount_rate")]
    public int DiscountRate { get; set; }

    [JsonPropertyName("discount_amount")]
    public int DiscountAmount { get; set; }

    [JsonPropertyName("fee_rate")]
    public int FeeRate { get; set; }

    [JsonPropertyName("fee_amount")]
    public int FeeAmount { get; set; }
}

/// <summary>
/// Item details
/// </summary>
public class Item
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("sellers_item_identification")]
    public object? SellersItemIdentification { get; set; }
}

/// <summary>
/// Price details
/// </summary>
public class Price
{
    [JsonPropertyName("price_amount")]
    public int PriceAmount { get; set; }

    [JsonPropertyName("base_quantity")]
    public int BaseQuantity { get; set; }

    [JsonPropertyName("price_unit")]
    public string PriceUnit { get; set; } = string.Empty;
}

/// <summary>
/// Tax information for an invoice line
/// </summary>
public sealed class PurchaseInvoiceLineTax
{
    /// <summary>
    /// Tax amount
    /// </summary>
    [JsonPropertyName("tax_amount")]
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Tax category
    /// </summary>
    [JsonPropertyName("tax_category_id")]
    public string? TaxCategoryId { get; set; }

    /// <summary>
    /// Tax percentage
    /// </summary>
    [JsonPropertyName("percent")]
    public decimal? Percent { get; set; }

    /// <summary>
    /// Tax scheme (e.g., VAT)
    /// </summary>
    [JsonPropertyName("tax_scheme_id")]
    public string? TaxSchemeId { get; set; }
}

/// <summary>
/// Tax total information for the entire invoice
/// </summary>
public sealed class PurchaseInvoiceTaxTotal
{
    /// <summary>
    /// Total tax amount
    /// </summary>
    [JsonPropertyName("tax_amount")]
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Tax subtotals by category
    /// </summary>
    [JsonPropertyName("tax_subtotal")]
    public List<PurchaseInvoiceTaxSubtotal> TaxSubtotal { get; set; } = new();
}

/// <summary>
/// Tax subtotal by category
/// </summary>
public sealed class PurchaseInvoiceTaxSubtotal
{
    /// <summary>
    /// Taxable amount
    /// </summary>
    [JsonPropertyName("taxable_amount")]
    public decimal TaxableAmount { get; set; }

    /// <summary>
    /// Tax amount
    /// </summary>
    [JsonPropertyName("tax_amount")]
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Tax category
    /// </summary>
    [JsonPropertyName("tax_category_id")]
    public string? TaxCategoryId { get; set; }

    /// <summary>
    /// Tax percentage
    /// </summary>
    [JsonPropertyName("percent")]
    public decimal? Percent { get; set; }

    /// <summary>
    /// Tax scheme
    /// </summary>
    [JsonPropertyName("tax_scheme_id")]
    public string? TaxSchemeId { get; set; }
}

/// <summary>
/// Pagination information for purchase invoices
/// </summary>
public sealed class PurchaseInvoicePage
{
    /// <summary>
    /// Current page number
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Total count of invoices
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>
/// Invoice delivery period information
/// </summary>
public sealed class PurchaseInvoiceDeliveryPeriod
{
    /// <summary>
    /// Start date of delivery period
    /// </summary>
    [JsonPropertyName("start_date")]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date of delivery period
    /// </summary>
    [JsonPropertyName("end_date")]
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Payment means information
/// </summary>
public sealed class PurchaseInvoicePaymentMeans
{
    /// <summary>
    /// Payment means code
    /// </summary>
    [JsonPropertyName("payment_means_code")]
    public string? PaymentMeansCode { get; set; }

    /// <summary>
    /// Payment due date
    /// </summary>
    [JsonPropertyName("payment_due_date")]
    public DateTime? PaymentDueDate { get; set; }

    /// <summary>
    /// Payment channel code
    /// </summary>
    [JsonPropertyName("payment_channel_code")]
    public string? PaymentChannelCode { get; set; }

    /// <summary>
    /// Payee financial account
    /// </summary>
    [JsonPropertyName("payee_financial_account")]
    public PurchaseInvoiceFinancialAccount? PayeeFinancialAccount { get; set; }
}

/// <summary>
/// Financial account information
/// </summary>
public sealed class PurchaseInvoiceFinancialAccount
{
    /// <summary>
    /// Account ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Account name
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Financial institution branch
    /// </summary>
    [JsonPropertyName("financial_institution_branch")]
    public PurchaseInvoiceFinancialInstitutionBranch? FinancialInstitutionBranch { get; set; }
}

/// <summary>
/// Financial institution branch information
/// </summary>
public sealed class PurchaseInvoiceFinancialInstitutionBranch
{
    /// <summary>
    /// Branch ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Branch name
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// Legal monetary total information
/// </summary>
public sealed class PurchaseInvoiceLegalMonetaryTotal
{
    /// <summary>
    /// Line extension amount (total before tax)
    /// </summary>
    [JsonPropertyName("line_extension_amount")]
    public decimal? LineExtensionAmount { get; set; }

    /// <summary>
    /// Tax exclusive amount
    /// </summary>
    [JsonPropertyName("tax_exclusive_amount")]
    public decimal? TaxExclusiveAmount { get; set; }

    /// <summary>
    /// Tax inclusive amount (total including tax)
    /// </summary>
    [JsonPropertyName("tax_inclusive_amount")]
    public decimal? TaxInclusiveAmount { get; set; }

    /// <summary>
    /// Allowance total amount
    /// </summary>
    [JsonPropertyName("allowance_total_amount")]
    public decimal? AllowanceTotalAmount { get; set; }

    /// <summary>
    /// Charge total amount
    /// </summary>
    [JsonPropertyName("charge_total_amount")]
    public decimal? ChargeTotalAmount { get; set; }

    /// <summary>
    /// Payable amount
    /// </summary>
    [JsonPropertyName("payable_amount")]
    public decimal? PayableAmount { get; set; }
}
