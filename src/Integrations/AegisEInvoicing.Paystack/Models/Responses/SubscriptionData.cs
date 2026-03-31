using System.Text.Json.Serialization;

namespace AegisEInvoicing.Paystack.Models.Responses;

public class SubscriptionData
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("subscription_code")]
    public string SubscriptionCode { get; set; } = string.Empty;

    [JsonPropertyName("email_token")]
    public string? EmailToken { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("cron_expression")]
    public string? CronExpression { get; set; }

    [JsonPropertyName("next_payment_date")]
    public DateTimeOffset? NextPaymentDate { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // active, non-renewing, cancelled

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("customer")]
    public PaystackCustomer? Customer { get; set; }

    [JsonPropertyName("plan")]
    public PlanData? Plan { get; set; }

    [JsonPropertyName("authorization")]
    public AuthorizationData? Authorization { get; set; }

    [JsonPropertyName("invoices")]
    public List<object>? Invoices { get; set; }

    [JsonPropertyName("invoices_history")]
    public List<object>? InvoicesHistory { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }
}

public class SubscriptionListData
{
    [JsonPropertyName("subscriptions")]
    public List<SubscriptionData> Subscriptions { get; set; } = [];

    [JsonPropertyName("meta")]
    public PaginationMeta? Meta { get; set; }
}

public class AuthorizationData
{
    [JsonPropertyName("authorization_code")]
    public string AuthorizationCode { get; set; } = string.Empty;

    [JsonPropertyName("bin")]
    public string? Bin { get; set; }

    [JsonPropertyName("last4")]
    public string? Last4 { get; set; }

    [JsonPropertyName("exp_month")]
    public string? ExpMonth { get; set; }

    [JsonPropertyName("exp_year")]
    public string? ExpYear { get; set; }

    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [JsonPropertyName("card_type")]
    public string? CardType { get; set; }

    [JsonPropertyName("bank")]
    public string? Bank { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("reusable")]
    public bool Reusable { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }

    [JsonPropertyName("account_name")]
    public string? AccountName { get; set; }
}

public class PaginationMeta
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("skipped")]
    public int Skipped { get; set; }

    [JsonPropertyName("perPage")]
    public int PerPage { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageCount")]
    public int PageCount { get; set; }
}
