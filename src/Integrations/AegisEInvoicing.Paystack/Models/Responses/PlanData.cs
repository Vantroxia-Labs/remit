using System.Text.Json.Serialization;

namespace AegisEInvoicing.Paystack.Models.Responses;

public class PlanData
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("plan_code")]
    public string PlanCode { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("interval")]
    public string Interval { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "NGN";

    [JsonPropertyName("invoice_limit")]
    public int? InvoiceLimit { get; set; }

    [JsonPropertyName("send_invoices")]
    public bool SendInvoices { get; set; }

    [JsonPropertyName("send_sms")]
    public bool SendSms { get; set; }

    [JsonPropertyName("hosted_page")]
    public bool HostedPage { get; set; }

    [JsonPropertyName("hosted_page_url")]
    public string? HostedPageUrl { get; set; }

    [JsonPropertyName("hosted_page_summary")]
    public string? HostedPageSummary { get; set; }

    [JsonPropertyName("is_archived")]
    public bool IsArchived { get; set; }

    [JsonPropertyName("migrate")]
    public bool? Migrate { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }
}

public class PlanListData
{
    [JsonPropertyName("subscriptions")]
    public List<PlanData> Plans { get; set; } = [];

    [JsonPropertyName("meta")]
    public PaginationMeta? Meta { get; set; }
}
