namespace AegisEInvoicing.Portal.API.Models.Invoice.Response;

public class ApprovalResponse
{
    public Guid InvoiceId { get; set; }
    public DateTimeOffset ApprovedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Comments { get; set; }
}
