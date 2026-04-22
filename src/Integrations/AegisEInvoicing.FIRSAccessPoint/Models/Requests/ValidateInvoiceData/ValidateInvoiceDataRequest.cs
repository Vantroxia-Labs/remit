using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Requests.ValidateInvoiceData;

public class AccountingCustomerParty
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
    public PostalAddress PostalAddress { get; set; } = null!;
}

public class AccountingSupplierParty
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
    public PostalAddress PostalAddress { get; set; } = null!;
}

public class AllowanceCharge
{
    [JsonPropertyName("charge_indicator")]
    public bool? ChargeIndicator { get; set; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }
}

public class BillingReference
{
    [JsonPropertyName("irn")]
    public string? Irn { get; set; }

    [JsonPropertyName("issue_date")]
    public DateOnly? IssueDate { get; set; }
}

public class ContractDocumentReference
{
    [JsonPropertyName("irn")]
    public string? Irn { get; set; }

    [JsonPropertyName("issue_date")]
    public DateOnly? IssueDate { get; set; }
}

public class DispatchDocumentReference
{
    [JsonPropertyName("irn")]
    public string? Irn { get; set; }

    [JsonPropertyName("issue_date")]
    public DateOnly? IssueDate { get; set; }
}

public class DocumentReference
{
    [JsonPropertyName("irn")]
    public string? Irn { get; set; }

    [JsonPropertyName("issue_date")]
    public DateOnly? IssueDate { get; set; }
}

public class InvoiceDeliveryPeriod
{
    [JsonPropertyName("start_date")]
    public DateOnly? StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateOnly? EndDate { get; set; }
}

public class InvoiceLine
{
    [JsonPropertyName("hsn_code")]
    public string? HsnCode { get; set; }

    [JsonPropertyName("product_category")]
    public string? ProductCategory { get; set; }

    [JsonPropertyName("isic_code")]
    public string? IsicCode { get; set; }

    [JsonPropertyName("service_category")]
    public string? ServiceCategory { get; set; }

    [JsonPropertyName("discount_rate")]
    public decimal DiscountRate { get; set; } = 0;

    [JsonPropertyName("discount_amount")]
    public decimal DiscountAmount { get; set; } = 0;

    [JsonPropertyName("fee_rate")]
    public decimal FeeRate { get; set; } = 0;

    [JsonPropertyName("fee_amount")]
    public decimal FeeAmount { get; set; } = 0;

    [JsonPropertyName("invoiced_quantity")]
    public decimal InvoicedQuantity { get; set; }

    [JsonPropertyName("line_extension_amount")]
    public decimal LineExtensionAmount { get; set; }

    [JsonPropertyName("item")]
    public Item Item { get; set; } = null!;

    [JsonPropertyName("price")]
    public Price Price { get; set; } = null!;
}

public class Item
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = null!;

    [JsonPropertyName("sellers_item_identification")]
    public string? SellersItemIdentification { get; set; }
}

public class LegalMonetaryTotal
{
    [JsonPropertyName("line_extension_amount")]
    public decimal LineExtensionAmount { get; set; }

    [JsonPropertyName("tax_exclusive_amount")]
    public decimal TaxExclusiveAmount { get; set; }

    [JsonPropertyName("tax_inclusive_amount")]
    public decimal TaxInclusiveAmount { get; set; }

    [JsonPropertyName("payable_amount")]
    public decimal PayableAmount { get; set; }
}

public class OriginatorDocumentReference
{
    [JsonPropertyName("irn")]
    public string? Irn { get; set; }

    [JsonPropertyName("issue_date")]
    public DateOnly? IssueDate { get; set; }
}

public class PaymentMean
{
    [JsonPropertyName("payment_means_code")]
    public string? PaymentMeansCode { get; set; }

    [JsonPropertyName("payment_due_date")]
    public DateOnly? PaymentDueDate { get; set; }
}

public class PostalAddress
{
    [JsonPropertyName("street_name")]
    public string StreetName { get; set; } = null!;

    [JsonPropertyName("city_name")]
    public string CityName { get; set; } = null!;

    [JsonPropertyName("postal_zone")]
    public string PostalZone { get; set; } = null!;

    [JsonPropertyName("country")]
    public string Country { get; set; } = null!;
}

public class Price
{
    [JsonPropertyName("price_amount")]
    public decimal PriceAmount { get; set; }

    [JsonPropertyName("base_quantity")]
    public decimal BaseQuantity { get; set; }

    [JsonPropertyName("price_unit")]
    public string PriceUnit { get; set; } = null!;
}

public class ReceiptDocumentReference
{
    [JsonPropertyName("irn")]
    public string? Irn { get; set; }

    [JsonPropertyName("issue_date")]
    public DateOnly? IssueDate { get; set; }
}

public sealed record ValidateInvoiceDataRequest
{
    [JsonPropertyName("business_id")]
    public string BusinessId { get; set; } = null!;

    [JsonPropertyName("irn")]
    public string Irn { get; set; } = null!;

    [JsonPropertyName("issue_date")]
    public DateOnly IssueDate { get; set; }

    [JsonPropertyName("due_date")]
    public DateOnly? DueDate { get; set; }

    [JsonPropertyName("issue_time")]
    public TimeOnly? IssueTime { get; set; }

    [JsonPropertyName("invoice_type_code")]
    public string InvoiceTypeCode { get; set; } = null!;

    [JsonPropertyName("invoice_kind")]
    public string? InvoiceKind { get; set; }

    [JsonPropertyName("payment_status")]
    public string PaymentStatus { get; set; } = "PENDING";

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("tax_point_date")]
    public DateOnly? TaxPointDate { get; set; }

    [JsonPropertyName("document_currency_code")]
    public string DocumentCurrencyCode { get; set; } = null!;

    [JsonPropertyName("tax_currency_code")]
    public string? TaxCurrencyCode { get; set; }

    [JsonPropertyName("accounting_cost")]
    public double? AccountingCost { get; set; }

    [JsonPropertyName("buyer_reference")]
    public string? BuyerReference { get; set; }

    [JsonPropertyName("invoice_delivery_period")]
    public InvoiceDeliveryPeriod? InvoiceDeliveryPeriod { get; set; }

    [JsonPropertyName("order_reference")]
    public string? OrderReference { get; set; }

    [JsonPropertyName("billing_reference")]
    public List<BillingReference>? BillingReference { get; set; } = [];

    [JsonPropertyName("dispatch_document_reference")]
    public DispatchDocumentReference? DispatchDocumentReference { get; set; }

    [JsonPropertyName("receipt_document_reference")]
    public ReceiptDocumentReference? ReceiptDocumentReference { get; set; }

    [JsonPropertyName("originator_document_reference")]
    public OriginatorDocumentReference? OriginatorDocumentReference { get; set; }

    [JsonPropertyName("contract_document_reference")]
    public ContractDocumentReference? ContractDocumentReference { get; set; }

    [JsonPropertyName("_document_reference")]
    public List<DocumentReference> DocumentReference { get; set; } = [];

    [JsonPropertyName("accounting_supplier_party")]
    public AccountingSupplierParty AccountingSupplierParty { get; set; } = null!;

    [JsonPropertyName("accounting_customer_party")]
    public AccountingCustomerParty AccountingCustomerParty { get; set; } = null!;

    [JsonPropertyName("actual_delivery_date")]
    public DateOnly? ActualDeliveryDate { get; set; }

    [JsonPropertyName("payment_means")]
    public List<PaymentMean> PaymentMeans { get; set; } = [];

    [JsonPropertyName("payment_terms_note")]
    public string? PaymentTermsNote { get; set; }

    [JsonPropertyName("allowance_charge")]
    public List<AllowanceCharge> AllowanceCharge { get; set; } = [];

    [JsonPropertyName("tax_total")]
    public List<TaxTotal> TaxTotal { get; set; } = [];

    [JsonPropertyName("legal_monetary_total")]
    public LegalMonetaryTotal LegalMonetaryTotal { get; set; } = null!;

    [JsonPropertyName("invoice_line")]
    public List<InvoiceLine> InvoiceLine { get; set; } = null!;
}

public class TaxCategory
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("percent")]
    public decimal? Percent { get; set; }
}

public class TaxSubtotal
{
    [JsonPropertyName("taxable_amount")]
    public decimal? TaxableAmount { get; set; }

    [JsonPropertyName("tax_amount")]
    public decimal? TaxAmount { get; set; }

    [JsonPropertyName("tax_category")]
    public TaxCategory? TaxCategory { get; set; }
}

public class TaxTotal
{
    [JsonPropertyName("tax_amount")]
    public decimal? TaxAmount { get; set; }

    [JsonPropertyName("tax_subtotal")]
    public List<TaxSubtotal> TaxSubtotal { get; set; } = [];
}