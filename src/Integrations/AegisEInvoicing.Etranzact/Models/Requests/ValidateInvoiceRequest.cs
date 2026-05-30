using System.Text.Json.Serialization;

namespace AegisEInvoicing.Etranzact.Models.Requests;

/// <summary>
/// Request to validate invoice structure and content prior to signing.
/// POST /api/v1/app/invoice/validate
/// </summary>
public sealed class ValidateInvoiceRequest
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

    /// <summary>
    /// Invoice kind: "B2B" or "B2C".
    /// </summary>
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
    public EtranzactInvoiceDeliveryPeriod? InvoiceDeliveryPeriod { get; set; }

    [JsonPropertyName("billing_reference")]
    public List<EtranzactBillingReference>? BillingReference { get; set; }

    [JsonPropertyName("dispatch_document_reference")]
    public EtranzactDocumentReference? DispatchDocumentReference { get; set; }

    [JsonPropertyName("receipt_document_reference")]
    public EtranzactDocumentReference? ReceiptDocumentReference { get; set; }

    [JsonPropertyName("originator_document_reference")]
    public EtranzactDocumentReference? OriginatorDocumentReference { get; set; }

    [JsonPropertyName("contract_document_reference")]
    public EtranzactDocumentReference? ContractDocumentReference { get; set; }

    [JsonPropertyName("additional_document_reference")]
    public List<EtranzactDocumentReference>? AdditionalDocumentReference { get; set; }

    [JsonPropertyName("accounting_supplier_party")]
    public EtranzactAccountingParty AccountingSupplierParty { get; set; } = null!;

    [JsonPropertyName("accounting_customer_party")]
    public EtranzactAccountingParty? AccountingCustomerParty { get; set; }

    [JsonPropertyName("payee_party")]
    public EtranzactAccountingParty? PayeeParty { get; set; }

    [JsonPropertyName("bill_party")]
    public EtranzactAccountingParty? BillParty { get; set; }

    [JsonPropertyName("ship_party")]
    public EtranzactAccountingParty? ShipParty { get; set; }

    [JsonPropertyName("tax_representative_party")]
    public EtranzactAccountingParty? TaxRepresentativeParty { get; set; }

    [JsonPropertyName("actual_delivery_date")]
    public DateOnly? ActualDeliveryDate { get; set; }

    [JsonPropertyName("payment_means")]
    public List<EtranzactPaymentMean>? PaymentMeans { get; set; }

    [JsonPropertyName("payment_terms_note")]
    public string? PaymentTermsNote { get; set; }

    [JsonPropertyName("allowance_charge")]
    public List<EtranzactAllowanceCharge>? AllowanceCharge { get; set; }

    [JsonPropertyName("tax_total")]
    public List<EtranzactTaxTotal> TaxTotal { get; set; } = [];

    [JsonPropertyName("legal_monetary_total")]
    public EtranzactLegalMonetaryTotal LegalMonetaryTotal { get; set; } = null!;

    [JsonPropertyName("invoice_line")]
    public List<EtranzactInvoiceLine> InvoiceLine { get; set; } = null!;
}
