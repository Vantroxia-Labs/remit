using System.Text.Json.Serialization;

namespace AegisEInvoicing.Paystack.Models.Requests;

public class CreatePlanRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; set; } // In kobo (multiply naira by 100)

    [JsonPropertyName("interval")]
    public string Interval { get; set; } = "monthly"; // hourly, daily, weekly, monthly, annually

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "NGN";

    [JsonPropertyName("invoice_limit")]
    public int? InvoiceLimit { get; set; }

    [JsonPropertyName("send_invoices")]
    public bool SendInvoices { get; set; } = false;

    [JsonPropertyName("send_sms")]
    public bool SendSms { get; set; } = false;
}
