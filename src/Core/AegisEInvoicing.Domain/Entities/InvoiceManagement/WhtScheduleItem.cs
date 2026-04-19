using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

/// <summary>
/// Snapshot of a received invoice's WHT data at the time the WHT schedule was generated.
/// One item per received invoice included in the schedule.
/// </summary>
public class WhtScheduleItem : Entity
{
    // FK back to the schedule
    public Guid ScheduleId { get; private set; }
    public WhtSchedule Schedule { get; private set; } = null!;

    // FK to the originating received invoice
    public Guid ReceivedInvoiceId { get; private set; }

    // Vendor / supplier details (snapshot at generation time)
    public string VendorName { get; private set; } = null!;
    public string? VendorAddress { get; private set; }
    public string? VendorTin { get; private set; }

    // Invoice identification
    public string Irn { get; private set; } = null!;
    public DateOnly IssueDate { get; private set; }

    /// <summary>How the transaction is classified for WHT purposes (e.g., Consultancy, SupplyOfGoods).</summary>
    public WhtNatureOfTransaction NatureOfTransaction { get; private set; }

    /// <summary>Gross payment amount before WHT deduction.</summary>
    public decimal GrossAmount { get; private set; }

    /// <summary>WHT rate applied, e.g. 5.0 for 5%.</summary>
    public decimal WhtRate { get; private set; }

    /// <summary>WHT amount deducted (GrossAmount × WhtRate / 100).</summary>
    public decimal WhtAmount { get; private set; }

    /// <summary>Net amount paid to supplier after WHT deduction.</summary>
    public decimal NetAmount => GrossAmount - WhtAmount;

    /// <summary>Whether WHT goes to NRS (B2B/B2G) or State IRS (individual payee / B2C).</summary>
    public WhtTaxAuthority TaxAuthority { get; private set; }

    // ── EF constructor ───────────────────────────────────────────────────────
    private WhtScheduleItem() { }

    // ── Static factory ───────────────────────────────────────────────────────
    public static WhtScheduleItem Create(
        Guid scheduleId,
        Guid receivedInvoiceId,
        string vendorName,
        string? vendorAddress,
        string? vendorTin,
        string irn,
        DateOnly issueDate,
        WhtNatureOfTransaction natureOfTransaction,
        decimal grossAmount,
        decimal whtRate,
        WhtTaxAuthority taxAuthority)
    {
        var whtAmount = Math.Round(grossAmount * whtRate / 100m, 2);

        return new WhtScheduleItem
        {
            ScheduleId = scheduleId,
            ReceivedInvoiceId = receivedInvoiceId,
            VendorName = vendorName,
            VendorAddress = vendorAddress,
            VendorTin = vendorTin,
            Irn = irn,
            IssueDate = issueDate,
            NatureOfTransaction = natureOfTransaction,
            GrossAmount = Math.Round(grossAmount, 2),
            WhtRate = whtRate,
            WhtAmount = whtAmount,
            TaxAuthority = taxAuthority,
        };
    }
}
