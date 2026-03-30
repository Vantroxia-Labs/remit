namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.ReportInvoice;

public sealed record ReportInvoiceResponse : GenericResponse
{
    public string InvoiceId { get; set; } = null!;
    public string Status { get; set; } = null!;
}