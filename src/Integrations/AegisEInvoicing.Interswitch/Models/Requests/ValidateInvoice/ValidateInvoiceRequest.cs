using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Models.Requests.ValidateInvoice;

/// <summary>
/// Request to validate invoice structure and content prior to signing
/// Uses the same payload structure as PostInvoice (UBL-compliant invoice)
/// </summary>
public sealed class ValidateInvoiceRequest
{
    /// <summary>
    /// Complete UBL-compliant invoice payload
    /// This should match your existing Invoice entity structure from FIRS integration
    /// </summary>
    [JsonPropertyName("invoice")]
    [Required]
    public object InvoicePayload { get; set; } = new();

    // Note: The actual invoice structure would match your FIRS ReportInvoiceRequest
    // You can reuse the same models or create a mapping
    // For now, using object to accept any invoice structure
    // In production, you should use a strongly-typed model like:
    // public ReportInvoiceRequest InvoicePayload { get; set; }
}
