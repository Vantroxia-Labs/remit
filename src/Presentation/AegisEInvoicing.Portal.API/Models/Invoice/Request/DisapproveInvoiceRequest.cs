using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.Invoice.Request;

/// <summary>
/// Request model for disapproving an invoice
/// </summary>
public class DisapproveInvoiceRequest
{
    /// <summary>
    /// Optional comments for the disapproval
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// Reason for rejecting the invoice
    /// </summary>
    [Required(ErrorMessage = "Rejection reason is required")]
    public string RejectionReason { get; set; } = string.Empty;
}
