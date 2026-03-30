namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetVatRemittanceReport;

public record VatRemittanceReportDto
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public List<VatRemittancePeriodDto> Periods { get; init; } = [];
    public decimal TotalTaxableAmount { get; init; }
    public decimal TotalVatAmount { get; init; }
    public int TotalInvoiceCount { get; init; }
}

public record VatRemittancePeriodDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = null!;
    public int InvoiceCount { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal VatAmount { get; init; }
}
