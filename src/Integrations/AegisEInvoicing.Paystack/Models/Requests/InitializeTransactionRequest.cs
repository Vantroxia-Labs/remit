using System.Text.Json.Serialization;

namespace AegisEInvoicing.Paystack.Models.Requests;

public class InitializeTransactionRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; set; } // In kobo (multiply naira by 100)

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "NGN";

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    [JsonPropertyName("metadata")]
    public PaystackMetadata? Metadata { get; set; }

    [JsonPropertyName("channels")]
    public List<string>? Channels { get; set; }
}

public class PaystackMetadata
{
    [JsonPropertyName("pending_registration_id")]
    public string? PendingRegistrationId { get; set; }

    [JsonPropertyName("plan_id")]
    public string? PlanId { get; set; }

    [JsonPropertyName("billing_cycle")]
    public string? BillingCycle { get; set; }

    [JsonPropertyName("business_name")]
    public string? BusinessName { get; set; }

    [JsonPropertyName("admin_email")]
    public string? AdminEmail { get; set; }

    [JsonPropertyName("custom_fields")]
    public List<PaystackCustomField>? CustomFields { get; set; }
}

public class PaystackCustomField
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("variable_name")]
    public string VariableName { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
