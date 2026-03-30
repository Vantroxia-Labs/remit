using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Responses.ConfirmInvoice;

public class ConfirmInvoiceResponse
{
    [JsonPropertyName("issue_date")]
    public string IssueDate { get; set; } = string.Empty;

    [JsonPropertyName("due_date")]
    public string DueDate { get; set; } = string.Empty;

    [JsonPropertyName("sync_date")]
    public string SyncDate { get; set; } = string.Empty;

    [JsonPropertyName("payment_status")]
    public string PaymentStatus { get; set; } = string.Empty;

    [JsonPropertyName("transmitted")]
    public bool Transmitted { get; set; }

    [JsonPropertyName("delivered")]
    public bool Delivered { get; set; }
}

public class ConfirmInvoiceWrappedResponse : InterswitchWrappedResponse<ConfirmInvoiceResponse> { }
