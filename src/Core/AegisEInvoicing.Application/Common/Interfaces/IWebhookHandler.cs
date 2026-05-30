namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Vendor-neutral representation of an invoice received via APP provider webhook.
/// Both BlueBridge and eTranzact push this shape when a transmitted invoice is confirmed.
/// </summary>
public sealed class ParsedWebhookInvoice
{
    public string Irn { get; init; } = string.Empty;
    public string InvoiceTypeCode { get; init; } = string.Empty;
    public string? IssueDate { get; init; }
    public string? IssueTime { get; init; }
    public string? DueDate { get; init; }
    public string DocumentCurrencyCode { get; init; } = string.Empty;
    public string? TaxCurrencyCode { get; init; }
    public string PaymentStatus { get; init; } = "PENDING";

    // Supplier
    public string SupplierPartyName { get; init; } = string.Empty;
    public string SupplierTin { get; init; } = string.Empty;
    public string? SupplierEmail { get; init; }
    public string? SupplierTelephone { get; init; }
    public string? SupplierStreet { get; init; }
    public string? SupplierCity { get; init; }
    public string? SupplierState { get; init; }
    public string? SupplierPostalCode { get; init; }
    public string? SupplierCountry { get; init; }

    // Customer
    public string CustomerPartyName { get; init; } = string.Empty;
    public string CustomerTin { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string? CustomerTelephone { get; init; }
    public string? CustomerStreet { get; init; }
    public string? CustomerCity { get; init; }
    public string? CustomerState { get; init; }
    public string? CustomerPostalCode { get; init; }
    public string? CustomerCountry { get; init; }

    // Monetary totals
    public decimal LineExtensionAmount { get; init; }
    public decimal TaxExclusiveAmount { get; init; }
    public decimal TaxInclusiveAmount { get; init; }
    public decimal TotalTaxAmount { get; init; }
    public decimal PayableAmount { get; init; }
    public decimal? PayableRoundingAmount { get; init; }

    // Optional fields
    public string? Note { get; init; }
    public string? BuyerReference { get; init; }
    public string? AccountingCost { get; init; }

    // Raw JSON blobs for downstream processing
    public string? InvoiceLinesJson { get; init; }
    public string? TaxTotalJson { get; init; }
    public string? RawPayload { get; init; }
}

/// <summary>
/// Verifies and parses incoming webhook deliveries from an APP provider.
/// Implemented by <c>BlueBridgeWebhookHandler</c> and <c>EtranzactWebhookHandler</c>.
/// The host resolves the correct handler via <see cref="ProviderCode"/> and passes
/// decrypted credentials from <c>AppProviderConfiguration</c>.
/// </summary>
public interface IWebhookHandler
{
    /// <summary>
    /// Lowercase, stable key matching <see cref="IAccessPointProviderClient.ProviderCode"/>.
    /// </summary>
    string ProviderCode { get; }

    /// <summary>
    /// Verifies the request authenticity and parses the webhook payload.
    /// Returns <c>(false, null)</c> on invalid signature or malformed body.
    /// </summary>
    Task<(bool Success, ParsedWebhookInvoice? Invoice)> VerifyAndParseAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        IReadOnlyDictionary<string, string> credentials,
        CancellationToken cancellationToken = default);
}
