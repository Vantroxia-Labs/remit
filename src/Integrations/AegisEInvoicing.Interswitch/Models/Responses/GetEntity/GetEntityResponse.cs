using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Responses.GetEntity;

/// <summary>
/// Response from GetEntity endpoint
/// </summary>
public sealed class GetEntityResponse : InterswitchResponse<EntityData>
{
}

public sealed class EntityData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;

    [JsonPropertyName("custom_settings")]
    public object? CustomSettings { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("businesses")]
    public List<BusinessInfo> Businesses { get; set; } = new();

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("app_reference")]
    public string AppReference { get; set; } = string.Empty;
}

public sealed class BusinessInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("custom_settings")]
    public object? CustomSettings { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("tin")]
    public string TIN { get; set; } = string.Empty;

    [JsonPropertyName("sector")]
    public string Sector { get; set; } = string.Empty;

    [JsonPropertyName("annual_turnover")]
    public string AnnualTurnover { get; set; } = string.Empty;

    [JsonPropertyName("support_peppol")]
    public bool SupportPeppol { get; set; }

    [JsonPropertyName("is_realtime_reporting")]
    public bool IsRealtimeReporting { get; set; }

    [JsonPropertyName("notification_channels")]
    public string NotificationChannels { get; set; } = string.Empty;

    [JsonPropertyName("erp_system")]
    public string ERPSystem { get; set; } = string.Empty;

    [JsonPropertyName("irn_template")]
    public string IRNTemplate { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
}
