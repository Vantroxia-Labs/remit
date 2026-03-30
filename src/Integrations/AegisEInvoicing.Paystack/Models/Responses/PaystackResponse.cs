using System.Text.Json.Serialization;

namespace AegisEInvoicing.Paystack.Models.Responses;

public class PaystackResponse<T>
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class InitializeTransactionData
{
    [JsonPropertyName("authorization_url")]
    public string AuthorizationUrl { get; set; } = string.Empty;

    [JsonPropertyName("access_code")]
    public string AccessCode { get; set; } = string.Empty;

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;
}

public class VerifyTransactionData
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
    public PaystackCustomer? Customer { get; set; }

    [JsonPropertyName("metadata")]
    public PaystackMetadataResponse? Metadata { get; set; }

    public bool IsSuccessful => Status == "success";
}

public class PaystackCustomer
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("customer_code")]
    public string CustomerCode { get; set; } = string.Empty;
}

public class PaystackMetadataResponse
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
}
