using System.Text.Json.Serialization;

namespace AegisEInvoicing.Infrastructure.Services.FirsMbs;

// These types are internal to FirsMbsApiClient for JSON deserialisation only.
// All public-facing data is returned as clean Application-layer DTOs.

internal record FirsMbsApiResponse<T>
{
    [JsonPropertyName("status")] public bool Status { get; init; }
    [JsonPropertyName("code")] public int Code { get; init; }
    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;
    [JsonPropertyName("data")] public T? Data { get; init; }
}

// Auth

internal record FirsMbsLoginRequest
{
    [JsonPropertyName("email")] public string Email { get; init; } = string.Empty;
    [JsonPropertyName("password")] public string Password { get; init; } = string.Empty;
    [JsonPropertyName("one_time_password")] public string OneTimePassword { get; init; } = string.Empty;
    [JsonPropertyName("token")] public string Token { get; init; } = string.Empty;
    [JsonPropertyName("role")] public string Role { get; init; } = string.Empty;
}

internal record FirsMbsLoginData
{
    [JsonPropertyName("tax_pro_max_token")] public string? TaxProMaxToken { get; init; }
    [JsonPropertyName("e_invoicing_token")] public string EInvoicingToken { get; init; } = string.Empty;
}

// Invoice list

internal record FirsMbsInvoiceListData
{
    [JsonPropertyName("items")] public List<FirsMbsInvoiceListItem> Items { get; init; } = [];
    [JsonPropertyName("page")] public FirsMbsPageInfo Page { get; init; } = new();
}

internal record FirsMbsInvoiceListItem
{
    [JsonPropertyName("irn")] public string Irn { get; init; } = string.Empty;
    [JsonPropertyName("entry_status")] public string EntryStatus { get; init; } = string.Empty;
    [JsonPropertyName("invoice_type_code")] public string InvoiceTypeCode { get; init; } = string.Empty;
    [JsonPropertyName("issue_date")] public string IssueDate { get; init; } = string.Empty;
    [JsonPropertyName("issue_time")] public string IssueTime { get; init; } = string.Empty;
    [JsonPropertyName("due_date")] public string? DueDate { get; init; }
    [JsonPropertyName("document_currency_code")] public string DocumentCurrencyCode { get; init; } = string.Empty;
}

internal record FirsMbsPageInfo
{
    [JsonPropertyName("page")] public int Page { get; init; }
    [JsonPropertyName("size")] public int Size { get; init; }
    [JsonPropertyName("hasNextPage")] public bool HasNextPage { get; init; }
    [JsonPropertyName("hasPreviousPage")] public bool HasPreviousPage { get; init; }
    [JsonPropertyName("totalCount")] public int TotalCount { get; init; }
}

// Invoice detail

internal record FirsMbsInvoiceDetail
{
    [JsonPropertyName("irn")] public string Irn { get; init; } = string.Empty;
    [JsonPropertyName("issue_date")] public string IssueDate { get; init; } = string.Empty;
    [JsonPropertyName("issue_time")] public string IssueTime { get; init; } = string.Empty;
    [JsonPropertyName("due_date")] public string? DueDate { get; init; }
    [JsonPropertyName("invoice_type_code")] public string InvoiceTypeCode { get; init; } = string.Empty;
    [JsonPropertyName("note")] public string? Note { get; init; }
    [JsonPropertyName("document_currency_code")] public string DocumentCurrencyCode { get; init; } = string.Empty;
    [JsonPropertyName("invoice_delivery_period")] public FirsMbsDeliveryPeriod? DeliveryPeriod { get; init; }
    [JsonPropertyName("accounting_supplier_party")] public FirsMbsParty? SupplierParty { get; init; }
    [JsonPropertyName("accounting_customer_party")] public FirsMbsParty? CustomerParty { get; init; }
    [JsonPropertyName("payment_means")] public List<FirsMbsPaymentMeans> PaymentMeans { get; init; } = [];
    [JsonPropertyName("payment_terms_note")] public string? PaymentTermsNote { get; init; }
    [JsonPropertyName("tax_total")] public List<FirsMbsTaxTotal> TaxTotal { get; init; } = [];
    [JsonPropertyName("invoice_line")] public List<FirsMbsInvoiceLine> InvoiceLine { get; init; } = [];
}

internal record FirsMbsDeliveryPeriod
{
    [JsonPropertyName("start_date")] public string StartDate { get; init; } = string.Empty;
    [JsonPropertyName("end_date")] public string EndDate { get; init; } = string.Empty;
}

internal record FirsMbsParty
{
    [JsonPropertyName("party_name")] public string PartyName { get; init; } = string.Empty;
    [JsonPropertyName("tin")] public string Tin { get; init; } = string.Empty;
    [JsonPropertyName("email")] public string Email { get; init; } = string.Empty;
    [JsonPropertyName("telephone")] public string Telephone { get; init; } = string.Empty;
    [JsonPropertyName("business_description")] public string? BusinessDescription { get; init; }
    [JsonPropertyName("postal_address")] public FirsMbsPostalAddress? PostalAddress { get; init; }
}

internal record FirsMbsPostalAddress
{
    [JsonPropertyName("street_name")] public string StreetName { get; init; } = string.Empty;
    [JsonPropertyName("city_name")] public string CityName { get; init; } = string.Empty;
    [JsonPropertyName("postal_zone")] public string? PostalZone { get; init; }
    [JsonPropertyName("state")] public string? State { get; init; }
    [JsonPropertyName("country")] public string Country { get; init; } = string.Empty;
}

internal record FirsMbsPaymentMeans
{
    [JsonPropertyName("payment_means_code")] public string PaymentMeansCode { get; init; } = string.Empty;
}

internal record FirsMbsTaxTotal
{
    [JsonPropertyName("tax_amount")] public decimal TaxAmount { get; init; }
    [JsonPropertyName("tax_subtotal")] public List<FirsMbsTaxSubtotal> TaxSubtotal { get; init; } = [];
}

internal record FirsMbsTaxSubtotal
{
    [JsonPropertyName("taxable_amount")] public decimal TaxableAmount { get; init; }
    [JsonPropertyName("tax_amount")] public decimal TaxAmount { get; init; }
    [JsonPropertyName("tax_category")] public FirsMbsTaxCategory? TaxCategory { get; init; }
}

internal record FirsMbsTaxCategory
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("percent")] public decimal Percent { get; init; }
}

internal record FirsMbsInvoiceLine
{
    [JsonPropertyName("invoiced_quantity")] public decimal InvoicedQuantity { get; init; }
    [JsonPropertyName("line_extension_amount")] public decimal LineExtensionAmount { get; init; }
    [JsonPropertyName("item")] public FirsMbsItem? Item { get; init; }
    [JsonPropertyName("price")] public FirsMbsPrice? Price { get; init; }
    [JsonPropertyName("hsn_code")] public string HsnCode { get; init; } = string.Empty;
    [JsonPropertyName("product_category")] public string ProductCategory { get; init; } = string.Empty;
    [JsonPropertyName("service_category")] public string ServiceCategory { get; init; } = string.Empty;
    [JsonPropertyName("discount_rate")] public decimal DiscountRate { get; init; }
    [JsonPropertyName("discount_amount")] public decimal DiscountAmount { get; init; }
    [JsonPropertyName("fee_rate")] public decimal FeeRate { get; init; }
    [JsonPropertyName("fee_amount")] public decimal FeeAmount { get; init; }
}

internal record FirsMbsItem
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; init; }
}

internal record FirsMbsPrice
{
    [JsonPropertyName("price_amount")] public decimal PriceAmount { get; init; }
    [JsonPropertyName("base_quantity")] public decimal BaseQuantity { get; init; }
}
