using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Requests.GetPurchaseInvoices;

/// <summary>
/// Request to fetch purchase invoices (received invoices) based on taxpayer TIN and date ranges
/// </summary>
public sealed class GetPurchaseInvoicesRequest
{
    /// <summary>
    /// Tax Identification Number of the taxpayer/business
    /// </summary>
    [JsonPropertyName("tin")]
    [Required]
    public string Tin { get; set; } = string.Empty;

    /// <summary>
    /// Start date for invoice query (Format: yyyy-MM-dd)
    /// </summary>
    [JsonPropertyName("startDate")]
    [Required]
    public string StartDate { get; set; } = string.Empty;

    /// <summary>
    /// End date for invoice query (Format: yyyy-MM-dd)
    /// </summary>
    [JsonPropertyName("endDate")]
    [Required]
    public string EndDate { get; set; } = string.Empty;
}