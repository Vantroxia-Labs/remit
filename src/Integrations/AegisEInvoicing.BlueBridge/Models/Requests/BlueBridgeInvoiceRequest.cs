using System.Text.Json.Serialization;

namespace AegisEInvoicing.BlueBridge.Models.Requests;

/// <summary>
/// Request body shared by both Validate Invoice and Sign Invoice endpoints.
/// POST /api/v1/invoices/validate
/// POST /api/v1/invoices/sign
/// </summary>
public sealed class BlueBridgeInvoiceRequest
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
    public string? AccountingCost { get; set; }

    [JsonPropertyName("buyer_reference")]
    public string? BuyerReference { get; set; }

    [JsonPropertyName("order_reference")]
    public string? OrderReference { get; set; }

    [JsonPropertyName("invoice_delivery_period")]
    public BlueBridgeInvoiceDeliveryPeriod? InvoiceDeliveryPeriod { get; set; }

    [JsonPropertyName("billing_reference")]
    public List<BlueBridgeBillingReference>? BillingReference { get; set; }

    [JsonPropertyName("dispatch_document_reference")]
    public BlueBridgeDocumentReference? DispatchDocumentReference { get; set; }

    [JsonPropertyName("receipt_document_reference")]
    public BlueBridgeDocumentReference? ReceiptDocumentReference { get; set; }

    [JsonPropertyName("originator_document_reference")]
    public BlueBridgeDocumentReference? OriginatorDocumentReference { get; set; }

    [JsonPropertyName("contract_document_reference")]
    public BlueBridgeDocumentReference? ContractDocumentReference { get; set; }

    [JsonPropertyName("additional_document_reference")]
    public List<BlueBridgeDocumentReference>? AdditionalDocumentReference { get; set; }

    [JsonPropertyName("accounting_supplier_party")]
    public BlueBridgeAccountingParty AccountingSupplierParty { get; set; } = null!;

    [JsonPropertyName("accounting_customer_party")]
    public BlueBridgeAccountingParty? AccountingCustomerParty { get; set; }

    [JsonPropertyName("payee_party")]
    public BlueBridgeAccountingParty? PayeeParty { get; set; }

    [JsonPropertyName("bill_party")]
    public BlueBridgeAccountingParty? BillParty { get; set; }

    [JsonPropertyName("ship_party")]
    public BlueBridgeAccountingParty? ShipParty { get; set; }

    [JsonPropertyName("tax_representative_party")]
    public BlueBridgeAccountingParty? TaxRepresentativeParty { get; set; }

    [JsonPropertyName("actual_delivery_date")]
    public DateOnly? ActualDeliveryDate { get; set; }

    [JsonPropertyName("payment_means")]
    public List<BlueBridgePaymentMean>? PaymentMeans { get; set; }

    [JsonPropertyName("payment_terms_note")]
    public string? PaymentTermsNote { get; set; }

    [JsonPropertyName("allowance_charge")]
    public List<BlueBridgeAllowanceCharge>? AllowanceCharge { get; set; }

    [JsonPropertyName("tax_total")]
    public List<BlueBridgeTaxTotal> TaxTotal { get; set; } = [];

    [JsonPropertyName("legal_monetary_total")]
    public BlueBridgeLegalMonetaryTotal LegalMonetaryTotal { get; set; } = null!;

    [JsonPropertyName("invoice_line")]
    public List<BlueBridgeInvoiceLine> InvoiceLine { get; set; } = null!;
}
