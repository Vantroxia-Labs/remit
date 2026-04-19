using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

/// <summary>
/// Snapshot of an invoice at the time the VAT schedule was generated.
/// Stored separately to ensure the schedule retains historically-accurate
/// amounts even if the source invoice is subsequently updated.
/// </summary>
public class VatScheduleItem : Entity
{
    // FK back to the schedule
    public Guid ScheduleId { get; private set; }
    public VatSchedule Schedule { get; private set; } = null!;

    // FK to the originating invoice (kept for drill-through; not navigated in queries)
    public Guid InvoiceId { get; private set; }

    // Denormalised snapshot fields
    public string InvoiceCode { get; private set; } = null!;
    public string? Irn { get; private set; }
    public string PartyName { get; private set; } = null!;
    public string? PartyTin { get; private set; }
    public DateOnly IssueDate { get; private set; }

    /// <summary>Sum of taxable line amounts (pre-VAT, post-discount).</summary>
    public decimal TaxableAmount { get; private set; }

    /// <summary>Total VAT on this invoice (rate depends on the applied tax category).</summary>
    public decimal VatAmount { get; private set; }

    public decimal TotalAmount => TaxableAmount + VatAmount;

    public PaymentStatus PaymentStatus { get; private set; }

    // ── EF constructor ───────────────────────────────────────────────────────
    private VatScheduleItem() { }

    // ── Static factory ───────────────────────────────────────────────────────
    /// <param name="scheduleId">Foreign key to the parent VAT Schedule.</param>
    /// <param name="invoiceId">Foreign key to the original invoice.</param>
    /// <param name="invoiceCode">The invoice number/code.</param>
    /// <param name="irn">The FIRS-issued Invoice Reference Number, if any.</param>
    /// <param name="partyName">The name of the counterparty (customer).</param>
    /// <param name="partyTin">The Tax Identification Number of the counterparty.</param>
    /// <param name="issueDate">The date the invoice was issued.</param>
    /// <param name="taxableAmount">Pre-VAT total — computed in the application layer from InvoiceItem projections.</param>
    /// <param name="vatAmount">Total VAT — computed in the application layer.</param>
    /// <param name="paymentStatus">The payment status of the invoice at the time of schedule generation.</param>
    public static VatScheduleItem Create(
        Guid scheduleId,
        Guid invoiceId,
        string invoiceCode,
        string? irn,
        string partyName,
        string? partyTin,
        DateOnly issueDate,
        decimal taxableAmount,
        decimal vatAmount,
        PaymentStatus paymentStatus)
    {
        return new VatScheduleItem
        {
            ScheduleId = scheduleId,
            InvoiceId = invoiceId,
            InvoiceCode = invoiceCode,
            Irn = irn,
            PartyName = partyName,
            PartyTin = partyTin,
            IssueDate = issueDate,
            TaxableAmount = Math.Round(taxableAmount, 2),
            VatAmount = Math.Round(vatAmount, 2),
            PaymentStatus = paymentStatus,
        };
    }
}
