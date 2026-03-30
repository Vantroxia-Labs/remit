using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Responses.LookupWithIRN;

/// <summary>
/// Response from LookupWithIRN endpoint
/// </summary>
public sealed class LookupWithIRNResponse : InterswitchWrappedResponse<BusinessParty>
{
}

public sealed class LookupData
{
    [JsonPropertyName("data")]
    public BusinessParty? AccountingCustomerParty { get; set; }
}

public sealed class BusinessParty
{
    [JsonPropertyName("app_reference")]
    public string AppReference { get; set; } = string.Empty;

    [JsonPropertyName("business_reference")]
    public string BusinessReference { get; set; } = string.Empty;

    [JsonPropertyName("business_erp_system")]
    public string BusinessERPSystem { get; set; } = string.Empty;

    [JsonPropertyName("business_tin")]
    public string BusinessTIN { get; set; } = string.Empty;

    [JsonPropertyName("business_sector")]
    public string BusinessSector { get; set; } = string.Empty;

    [JsonPropertyName("has_webhook_setup")]
    public bool HasWebhookSetup { get; set; }

    [JsonPropertyName("up")]
    public bool IsUp { get; set; }
}
