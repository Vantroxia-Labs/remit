using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Responses.SearchInvoice;

/// <summary>
/// Response from SearchInvoice endpoint
/// </summary>
public sealed class SearchInvoiceResponse : InterswitchResponse<SearchInvoiceData>
{
}

public sealed class SearchInvoiceData
{
    [JsonPropertyName("items")]
    public List<InvoiceItem> Items { get; set; } = new();

    [JsonPropertyName("page")]
    public PageInfo Page { get; set; } = new();

    [JsonPropertyName("attributes")]
    public object? Attributes { get; set; }
}

public sealed class InvoiceItem
{
    [JsonPropertyName("irn")]
    public string IRN { get; set; } = string.Empty;

    [JsonPropertyName("payment_status")]
    public string PaymentStatus { get; set; } = string.Empty;

    [JsonPropertyName("entry_status")]
    public string EntryStatus { get; set; } = string.Empty;

    [JsonPropertyName("invoice_type_code")]
    public string InvoiceTypeCode { get; set; } = string.Empty;

    [JsonPropertyName("issue_date")]
    public DateTime IssueDate { get; set; }

    [JsonPropertyName("issue_time")]
    public string IssueTime { get; set; } = string.Empty;

    [JsonPropertyName("due_date")]
    public DateTime DueDate { get; set; }

    [JsonPropertyName("sync_date")]
    public string SyncDate { get; set; } = string.Empty;

    [JsonPropertyName("document_currency_code")]
    public string DocumentCurrencyCode { get; set; } = string.Empty;

    [JsonPropertyName("tax_currency_code")]
    public string TaxCurrencyCode { get; set; } = string.Empty;
}

public sealed class PageInfo
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}
