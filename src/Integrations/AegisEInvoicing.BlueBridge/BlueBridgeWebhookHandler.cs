using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.BlueBridge;

/// <summary>
/// Verifies and parses incoming webhook requests from BlueBridge.
/// Checks the X-API-Key using the credentials resolved by the host, then maps the
/// payload to a vendor-neutral <see cref="ParsedWebhookInvoice"/>.
/// BlueBridge uses only an API key for webhook auth (no HMAC signature).
/// </summary>
public sealed class BlueBridgeWebhookHandler(ILogger<BlueBridgeWebhookHandler> logger)
    : IWebhookHandler
{
    public string ProviderCode => "bluebridge";

    public Task<(bool Success, ParsedWebhookInvoice? Invoice)> VerifyAndParseAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        IReadOnlyDictionary<string, string> credentials,
        CancellationToken cancellationToken = default)
    {
        headers.TryGetValue("X-API-Key", out var apiKey);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("BlueBridge webhook rejected: missing X-API-Key header");
            return Task.FromResult<(bool, ParsedWebhookInvoice?)>((false, null));
        }

        // Verify the API key using constant-time comparison to prevent timing attacks.
        credentials.TryGetValue("ClientApiKey", out var expectedApiKey);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(apiKey),
                Encoding.UTF8.GetBytes(expectedApiKey ?? string.Empty)))
        {
            logger.LogWarning("BlueBridge webhook rejected: X-API-Key mismatch");
            return Task.FromResult<(bool, ParsedWebhookInvoice?)>((false, null));
        }

        BlueBridgeWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<BlueBridgeWebhookPayload>(rawBody, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "BlueBridge webhook: failed to deserialize payload");
            return Task.FromResult<(bool, ParsedWebhookInvoice?)>((false, null));
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Irn))
        {
            logger.LogWarning("BlueBridge webhook: payload missing required IRN");
            return Task.FromResult<(bool, ParsedWebhookInvoice?)>((false, null));
        }

        var customerTin = payload.AccountingCustomerParty?.Tin;
        if (string.IsNullOrWhiteSpace(customerTin))
        {
            logger.LogWarning("BlueBridge webhook IRN={Irn}: missing customer TIN", payload.Irn);
        }

        var supplier = payload.AccountingSupplierParty;
        var customer = payload.AccountingCustomerParty;
        var totals = payload.LegalMonetaryTotal;

        var parsed = new ParsedWebhookInvoice
        {
            Irn = payload.Irn,
            InvoiceTypeCode = payload.InvoiceTypeCode ?? string.Empty,
            IssueDate = payload.IssueDate.ToString("yyyy-MM-dd"),
            IssueTime = payload.IssueTime?.ToString("HH:mm:ss"),
            DueDate = payload.DueDate?.ToString("yyyy-MM-dd"),
            DocumentCurrencyCode = payload.DocumentCurrencyCode ?? string.Empty,
            TaxCurrencyCode = payload.TaxCurrencyCode,
            PaymentStatus = payload.PaymentStatus ?? "PENDING",
            SupplierPartyName = supplier?.PartyName ?? string.Empty,
            SupplierTin = supplier?.Tin ?? string.Empty,
            SupplierEmail = supplier?.Email,
            SupplierTelephone = supplier?.Telephone,
            SupplierStreet = supplier?.PostalAddress?.StreetName,
            SupplierCity = supplier?.PostalAddress?.CityName,
            SupplierState = supplier?.PostalAddress?.State,
            SupplierPostalCode = supplier?.PostalAddress?.PostalZone,
            SupplierCountry = supplier?.PostalAddress?.Country,
            CustomerPartyName = customer?.PartyName ?? string.Empty,
            CustomerTin = customerTin ?? string.Empty,
            CustomerEmail = customer?.Email,
            CustomerTelephone = customer?.Telephone,
            CustomerStreet = customer?.PostalAddress?.StreetName,
            CustomerCity = customer?.PostalAddress?.CityName,
            CustomerState = customer?.PostalAddress?.State,
            CustomerPostalCode = customer?.PostalAddress?.PostalZone,
            CustomerCountry = customer?.PostalAddress?.Country,
            LineExtensionAmount = (decimal)(totals?.LineExtensionAmount ?? 0),
            TaxExclusiveAmount = (decimal)(totals?.TaxExclusiveAmount ?? 0),
            TaxInclusiveAmount = (decimal)(totals?.TaxInclusiveAmount ?? 0),
            TotalTaxAmount = payload.TaxTotal?.Sum(t => (decimal)t.TaxAmount) ?? 0m,
            PayableAmount = (decimal)(totals?.PayableAmount ?? 0),
            PayableRoundingAmount = totals?.PayableRoundingAmount.HasValue == true
                                        ? (decimal?)totals.PayableRoundingAmount.Value : null,
            Note = payload.Note,
            BuyerReference = payload.BuyerReference,
            AccountingCost = payload.AccountingCost,
            InvoiceLinesJson = payload.InvoiceLine is not null
                                        ? JsonSerializer.Serialize(payload.InvoiceLine, JsonOptions) : null,
            TaxTotalJson = payload.TaxTotal is not null
                                        ? JsonSerializer.Serialize(payload.TaxTotal, JsonOptions) : null,
            RawPayload = rawBody,
        };

        return Task.FromResult<(bool, ParsedWebhookInvoice?)>((true, parsed));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}

// BlueBridge webhook payload DTOs — mirrors the UBL invoice structure sent by BlueBridge.

internal sealed class BlueBridgeWebhookPayload
{
    [JsonPropertyName("irn")]
    public string Irn { get; set; } = string.Empty;

    [JsonPropertyName("invoice_type_code")]
    public string? InvoiceTypeCode { get; set; }

    [JsonPropertyName("issue_date")]
    public DateOnly IssueDate { get; set; }

    [JsonPropertyName("issue_time")]
    public TimeOnly? IssueTime { get; set; }

    [JsonPropertyName("due_date")]
    public DateOnly? DueDate { get; set; }

    [JsonPropertyName("document_currency_code")]
    public string? DocumentCurrencyCode { get; set; }

    [JsonPropertyName("tax_currency_code")]
    public string? TaxCurrencyCode { get; set; }

    [JsonPropertyName("payment_status")]
    public string? PaymentStatus { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("buyer_reference")]
    public string? BuyerReference { get; set; }

    [JsonPropertyName("accounting_cost")]
    public string? AccountingCost { get; set; }

    [JsonPropertyName("accounting_supplier_party")]
    public BlueBridgeWebhookParty? AccountingSupplierParty { get; set; }

    [JsonPropertyName("accounting_customer_party")]
    public BlueBridgeWebhookParty? AccountingCustomerParty { get; set; }

    [JsonPropertyName("legal_monetary_total")]
    public BlueBridgeWebhookMonetaryTotal? LegalMonetaryTotal { get; set; }

    [JsonPropertyName("tax_total")]
    public List<BlueBridgeWebhookTaxTotal>? TaxTotal { get; set; }

    [JsonPropertyName("invoice_line")]
    public List<JsonElement>? InvoiceLine { get; set; }
}

internal sealed class BlueBridgeWebhookParty
{
    [JsonPropertyName("party_name")]
    public string? PartyName { get; set; }

    [JsonPropertyName("tin")]
    public string? Tin { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("telephone")]
    public string? Telephone { get; set; }

    [JsonPropertyName("postal_address")]
    public BlueBridgeWebhookAddress? PostalAddress { get; set; }
}

internal sealed class BlueBridgeWebhookAddress
{
    [JsonPropertyName("street_name")]
    public string? StreetName { get; set; }

    [JsonPropertyName("city_name")]
    public string? CityName { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("postal_zone")]
    public string? PostalZone { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

internal sealed class BlueBridgeWebhookMonetaryTotal
{
    [JsonPropertyName("line_extension_amount")]
    public double LineExtensionAmount { get; set; }

    [JsonPropertyName("tax_exclusive_amount")]
    public double TaxExclusiveAmount { get; set; }

    [JsonPropertyName("tax_inclusive_amount")]
    public double TaxInclusiveAmount { get; set; }

    [JsonPropertyName("payable_amount")]
    public double PayableAmount { get; set; }

    [JsonPropertyName("payable_rounding_amount")]
    public double? PayableRoundingAmount { get; set; }
}

internal sealed class BlueBridgeWebhookTaxTotal
{
    [JsonPropertyName("tax_amount")]
    public double TaxAmount { get; set; }
}
