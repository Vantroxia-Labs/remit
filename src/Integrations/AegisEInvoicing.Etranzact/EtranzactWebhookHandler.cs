using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Etranzact;

/// <summary>
/// Verifies and parses incoming webhook requests from eTranzact.
/// Checks the X-API-Key and HMAC-SHA256 signature using the credentials resolved
/// by the host, then maps the payload to a vendor-neutral <see cref="ParsedWebhookInvoice"/>.
/// </summary>
public sealed class EtranzactWebhookHandler(ILogger<EtranzactWebhookHandler> logger)
    : IWebhookHandler
{
    public string ProviderCode => "etranzact";

    public Task<(bool Success, ParsedWebhookInvoice? Invoice)> VerifyAndParseAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        IReadOnlyDictionary<string, string> credentials,
        CancellationToken cancellationToken = default)
    {
        headers.TryGetValue("X-API-Key", out var apiKey);
        headers.TryGetValue("X-Signature", out var signature);
        headers.TryGetValue("X-Timestamp", out var timestamp);

        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(signature) ||
            string.IsNullOrWhiteSpace(timestamp))
        {
            logger.LogWarning("eTranzact webhook rejected: missing required headers (X-API-Key, X-Signature, X-Timestamp)");
            return Task.FromResult<(bool, ParsedWebhookInvoice?)>((false, null));
        }

        // Verify the API key using constant-time comparison to prevent timing attacks.
        credentials.TryGetValue("ClientApiKey", out var expectedApiKey);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(apiKey),
                Encoding.UTF8.GetBytes(expectedApiKey ?? string.Empty)))
        {
            logger.LogWarning("eTranzact webhook rejected: X-API-Key mismatch");
            return Task.FromResult<(bool, ParsedWebhookInvoice?)>((false, null));
        }

        // Verify the HMAC-SHA256 signature: Base64( HMAC-SHA256( ClientSecretKey, rawBody + timestamp ) )
        credentials.TryGetValue("ClientSecretKey", out var secretKey);
        var expectedSignature = ComputeHmac(secretKey ?? string.Empty, rawBody + timestamp);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            logger.LogWarning("eTranzact webhook rejected: invalid X-Signature");
            return Task.FromResult<(bool, ParsedWebhookInvoice?)>((false, null));
        }

        EtranzactWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<EtranzactWebhookPayload>(rawBody, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "eTranzact webhook: failed to deserialize payload");
            return Task.FromResult<(bool, ParsedWebhookInvoice?)>((false, null));
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Irn))
        {
            logger.LogWarning("eTranzact webhook: payload missing required IRN");
            return Task.FromResult<(bool, ParsedWebhookInvoice?)>((false, null));
        }

        var customerTin = payload.AccountingCustomerParty?.Tin;
        if (string.IsNullOrWhiteSpace(customerTin))
        {
            logger.LogWarning("eTranzact webhook IRN={Irn}: missing customer TIN", payload.Irn);
            // Return success so the ingestion pipeline can acknowledge and handle the missing TIN
            // gracefully rather than causing eTranzact to retry the delivery.
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

    private static string ComputeHmac(string secretKey, string message)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToBase64String(hmac.ComputeHash(messageBytes));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}

// eTranzact-specific payload DTOs — mirror the validate/sign request structure.

internal sealed class EtranzactWebhookPayload
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
    public EtranzactWebhookParty? AccountingSupplierParty { get; set; }

    [JsonPropertyName("accounting_customer_party")]
    public EtranzactWebhookParty? AccountingCustomerParty { get; set; }

    [JsonPropertyName("legal_monetary_total")]
    public EtranzactWebhookMonetaryTotal? LegalMonetaryTotal { get; set; }

    [JsonPropertyName("tax_total")]
    public List<EtranzactWebhookTaxTotal>? TaxTotal { get; set; }

    [JsonPropertyName("invoice_line")]
    public List<JsonElement>? InvoiceLine { get; set; }
}

internal sealed class EtranzactWebhookParty
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
    public EtranzactWebhookAddress? PostalAddress { get; set; }
}

internal sealed class EtranzactWebhookAddress
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

internal sealed class EtranzactWebhookMonetaryTotal
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

internal sealed class EtranzactWebhookTaxTotal
{
    [JsonPropertyName("tax_amount")]
    public double TaxAmount { get; set; }
}
