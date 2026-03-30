namespace AegisEInvoicing.Application.Features.VatScheduleManagement.VatScheduleExport.Dto
{
    public class VatScheduleItemExportDto
    {
        public string? InvoiceCode { get; set; }
        public string? IRN { get; set; }
        public string? PartyName { get; set; }
        public string? PartyTin { get; set; }
        public DateOnly IssueDate { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? PaymentStatus { get; set; }
    }
}
