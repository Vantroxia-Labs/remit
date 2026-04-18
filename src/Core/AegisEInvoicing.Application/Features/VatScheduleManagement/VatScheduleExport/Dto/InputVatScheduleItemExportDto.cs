namespace AegisEInvoicing.Application.Features.VatScheduleManagement.VatScheduleExport.Dto;

public class InputVatScheduleItemExportDto
{
    public string Irn { get; set; } = null!;
    public string SupplierName { get; set; } = null!;
    public string? SupplierTin { get; set; }
    public DateOnly IssueDate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
}
