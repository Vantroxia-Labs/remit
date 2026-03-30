namespace AegisEInvoicing.FIRSAccessPoint.Models.Requests.ReportInvoice;

public sealed record ReportInvoiceRequest
{
    public string InvoiceNumber { get; set; } = null!;
    public DateTime InvoiceDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string CustomerTaxId { get; set; } = null!;
}