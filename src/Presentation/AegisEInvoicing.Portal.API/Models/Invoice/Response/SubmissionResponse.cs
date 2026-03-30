namespace AegisEInvoicing.Portal.API.Models.Invoice.Response;

public class SubmissionResponse
{
    public Guid InvoiceId { get; set; }
    public string FIRSSubmissionId { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
