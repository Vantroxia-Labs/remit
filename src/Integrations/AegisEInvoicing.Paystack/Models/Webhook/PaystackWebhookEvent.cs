using System.Text.Json.Serialization;

namespace AegisEInvoicing.Paystack.Models.Webhook;

public class PaystackWebhookEvent
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public PaystackWebhookData? Data { get; set; }
}

public class PaystackWebhookData
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("paid_at")]
    public DateTimeOffset? PaidAt { get; set; }

    [JsonPropertyName("customer")]
    public PaystackWebhookCustomer? Customer { get; set; }

    [JsonPropertyName("metadata")]
    public PaystackWebhookMetadata? Metadata { get; set; }
}

public class PaystackWebhookCustomer
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

public class PaystackWebhookMetadata
{
    [JsonPropertyName("pending_registration_id")]
    public string? PendingRegistrationId { get; set; }

    [JsonPropertyName("plan_id")]
    public string? PlanId { get; set; }

    [JsonPropertyName("billing_cycle")]
    public string? BillingCycle { get; set; }
}

public static class PaystackEvents
{
    public const string ChargeSuccess = "charge.success";
    public const string SubscriptionCreate = "subscription.create";
    public const string InvoiceCreate = "invoice.create";
    public const string InvoiceUpdate = "invoice.update";
    public const string TransferSuccess = "transfer.success";
    public const string TransferFailed = "transfer.failed";
}
