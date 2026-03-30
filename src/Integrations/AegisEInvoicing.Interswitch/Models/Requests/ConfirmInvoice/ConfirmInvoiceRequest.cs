using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Requests.ConfirmInvoice;

public class ConfirmInvoiceRequest
{
    /// <summary>
    /// Invoice Reference Number of the recieved invoice
    /// </summary>
    [JsonPropertyName("IRN")]
    [Required]
    public string IRN { get; set; } = string.Empty;
}
