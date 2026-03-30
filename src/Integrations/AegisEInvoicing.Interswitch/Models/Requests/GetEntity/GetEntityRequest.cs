using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Requests.GetEntity;

/// <summary>
/// Request to fetch taxpayer entity information
/// </summary>
public sealed class GetEntityRequest
{
    /// <summary>
    /// Entity identifier (UUID)
    /// </summary>
    [JsonPropertyName("EntityId")]
    [Required]
    public string EntityId { get; set; } = string.Empty;
}
