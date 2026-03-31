using System.Text.Json.Serialization;

namespace AegisEInvoicing.Paystack.Models.Requests;

public class CreateSubscriptionRequest
{
    [JsonPropertyName("customer")]
    public string Customer { get; set; } = string.Empty; // Customer ID or code

    [JsonPropertyName("plan")]
    public string Plan { get; set; } = string.Empty; // Plan ID or code

    [JsonPropertyName("authorization")]
    public string? Authorization { get; set; } // Authorization code (if customer has been charged before)

    [JsonPropertyName("start_date")]
    public DateTimeOffset? StartDate { get; set; }
}
