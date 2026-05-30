using System.Text.Json.Serialization;

namespace AegisEInvoicing.BlueBridge.Models.Requests;

/// <summary>
/// Shared UBL-compliant types used across BlueBridge invoice request payloads.
/// </summary>

public class BlueBridgePostalAddress
{
    [JsonPropertyName("street_name")]
    public string StreetName { get; set; } = null!;

    [JsonPropertyName("city_name")]
    public string CityName { get; set; } = null!;

    [JsonPropertyName("lga")]
    public string? Lga { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("postal_zone")]
    public string PostalZone { get; set; } = null!;

    [JsonPropertyName("country")]
    public string Country { get; set; } = null!;
}

public class BlueBridgeAccountingParty
{
    [JsonPropertyName("party_name")]
    public string PartyName { get; set; } = null!;

    [JsonPropertyName("tin")]
    public string Tin { get; set; } = null!;

    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    [JsonPropertyName("telephone")]
    public string? Telephone { get; set; }

    [JsonPropertyName("business_description")]
    public string? BusinessDescription { get; set; }

    [JsonPropertyName("postal_address")]
    public BlueBridgePostalAddress PostalAddress { get; set; } = null!;
}

public class BlueBridgeInvoiceLine
{
    [JsonPropertyName("item")]
    public BlueBridgeItem Item { get; set; } = null!;

    [JsonPropertyName("price")]
    public BlueBridgePrice Price { get; set; } = null!;

    [JsonPropertyName("hsn_code")]
    public string? HsnCode { get; set; }

    [JsonPropertyName("product_category")]
    public string? ProductCategory { get; set; }

    [JsonPropertyName("isic_code")]
    public string? IsicCode { get; set; }

    [JsonPropertyName("service_category")]
    public string? ServiceCategory { get; set; }

    [JsonPropertyName("discount_rate")]
    public double? DiscountRate { get; set; }

    [JsonPropertyName("discount_amount")]
    public double? DiscountAmount { get; set; }

    [JsonPropertyName("fee_rate")]
    public double? FeeRate { get; set; }

    [JsonPropertyName("fee_amount")]
    public double? FeeAmount { get; set; }

    [JsonPropertyName("invoiced_quantity")]
    public int InvoicedQuantity { get; set; }

    [JsonPropertyName("line_extension_amount")]
    public double LineExtensionAmount { get; set; }
}

public class BlueBridgeItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = null!;

    [JsonPropertyName("sellers_item_identification")]
    public string? SellersItemIdentification { get; set; }
}

public class BlueBridgePrice
{
    [JsonPropertyName("price_amount")]
    public double PriceAmount { get; set; }

    [JsonPropertyName("base_quantity")]
    public int BaseQuantity { get; set; }

    [JsonPropertyName("price_unit")]
    public string? PriceUnit { get; set; }
}

public class BlueBridgeLegalMonetaryTotal
{
    [JsonPropertyName("line_extension_amount")]
    public double LineExtensionAmount { get; set; }

    [JsonPropertyName("tax_exclusive_amount")]
    public double TaxExclusiveAmount { get; set; }

    [JsonPropertyName("tax_inclusive_amount")]
    public double TaxInclusiveAmount { get; set; }

    [JsonPropertyName("payable_amount")]
    public double PayableAmount { get; set; }
}

public class BlueBridgeTaxTotal
{
    [JsonPropertyName("tax_amount")]
    public double? TaxAmount { get; set; }

    [JsonPropertyName("tax_subtotal")]
    public List<BlueBridgeTaxSubtotal> TaxSubtotal { get; set; } = [];
}

public class BlueBridgeTaxSubtotal
{
    [JsonPropertyName("taxable_amount")]
    public double? TaxableAmount { get; set; }

    [JsonPropertyName("tax_amount")]
    public double? TaxAmount { get; set; }

    [JsonPropertyName("tax_category")]
    public BlueBridgeTaxCategory? TaxCategory { get; set; }

    [JsonPropertyName("tax_category_percent")]
    public double? TaxCategoryPercent { get; set; }
}

public class BlueBridgeTaxCategory
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("percent")]
    public double? Percent { get; set; }

    [JsonPropertyName("tax_scheme")]
    public BlueBridgeTaxScheme? TaxScheme { get; set; }
}

public class BlueBridgeTaxScheme
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class BlueBridgeAllowanceCharge
{
    [JsonPropertyName("charge_indicator")]
    public bool ChargeIndicator { get; set; }

    [JsonPropertyName("amount")]
    public double Amount { get; set; }
}

public class BlueBridgeInvoiceDeliveryPeriod
{
    [JsonPropertyName("start_date")]
    public DateOnly? StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateOnly? EndDate { get; set; }
}

public class BlueBridgePaymentMean
{
    [JsonPropertyName("payment_means_code")]
    public string? PaymentMeansCode { get; set; }

    [JsonPropertyName("payment_due_date")]
    public DateOnly? PaymentDueDate { get; set; }
}

public class BlueBridgeBillingReference
{
    [JsonPropertyName("irn")]
    public string? Irn { get; set; }

    [JsonPropertyName("issue_date")]
    public DateOnly? IssueDate { get; set; }
}

public class BlueBridgeDocumentReference
{
    [JsonPropertyName("irn")]
    public string? Irn { get; set; }

    [JsonPropertyName("issue_date")]
    public DateOnly? IssueDate { get; set; }
}
