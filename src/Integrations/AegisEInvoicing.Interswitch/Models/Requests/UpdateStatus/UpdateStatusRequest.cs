using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Requests.UpdateStatus;

/// <summary>
/// Request to update invoice payment status
/// </summary>
public sealed class UpdateStatusRequest
{
    /// <summary>
    /// Payment status (e.g., "PAID", "UNPAID", "PARTIALLY_PAID")
    /// </summary>
    [JsonPropertyName("payment_status")]
    [Required]
    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>
    /// Payment reference or notes
    /// </summary>
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    /// <summary>
    /// Invoice Reference Number
    /// </summary>
    [JsonPropertyName("irn")]
    [Required]
    public string IRN { get; set; } = string.Empty;
}
