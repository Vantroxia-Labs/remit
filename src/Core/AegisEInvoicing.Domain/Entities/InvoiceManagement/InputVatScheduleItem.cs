using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

/// <summary>
/// Snapshot of a received (purchase) invoice's VAT data at the time the VAT schedule was generated.
/// Only VAT-category subtotals (STANDARD_VAT, REDUCED_VAT, ZERO_VAT) are captured here.
/// </summary>
public class InputVatScheduleItem : Entity
{
    public Guid ScheduleId { get; private set; }
    public VatSchedule Schedule { get; private set; } = null!;

    public Guid ReceivedInvoiceId { get; private set; }

    public string Irn { get; private set; } = null!;
    public string SupplierName { get; private set; } = null!;
    public string? SupplierTin { get; private set; }
    public DateOnly IssueDate { get; private set; }

    /// <summary>VAT-taxable base amount extracted from TaxTotalJson subtotals.</summary>
    public decimal TaxableAmount { get; private set; }

    /// <summary>VAT amount extracted from TaxTotalJson subtotals (STANDARD_VAT / REDUCED_VAT / ZERO_VAT).</summary>
    public decimal VatAmount { get; private set; }

    public decimal TotalAmount => TaxableAmount + VatAmount;

    private InputVatScheduleItem() { }

    public static InputVatScheduleItem Create(
        Guid scheduleId,
        Guid receivedInvoiceId,
        string irn,
        string supplierName,
        string? supplierTin,
        DateOnly issueDate,
        decimal taxableAmount,
        decimal vatAmount)
    {
        return new InputVatScheduleItem
        {
            ScheduleId = scheduleId,
            ReceivedInvoiceId = receivedInvoiceId,
            Irn = irn,
            SupplierName = supplierName,
            SupplierTin = supplierTin,
            IssueDate = issueDate,
            TaxableAmount = Math.Round(taxableAmount, 2),
            VatAmount = Math.Round(vatAmount, 2),
        };
    }
}
