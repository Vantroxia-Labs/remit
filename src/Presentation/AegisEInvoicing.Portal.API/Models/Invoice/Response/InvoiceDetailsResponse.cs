namespace AegisEInvoicing.Portal.API.Models.Invoice.Response;

public class InvoiceDetailsResponse
{
    public Guid InvoiceId { get; set; }
    public string InvoiceReferenceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}