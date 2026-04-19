using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

/// <summary>
/// A point-in-time monthly output-VAT schedule for a business.
/// One schedule is generated per calendar month; it captures all transmitted
/// invoices that were not already included in any previous schedule.
/// The due filing date is always the 21st of the following month.
/// </summary>
public class VatSchedule : AuditableAggregateRoot
{
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string MonthName { get; private set; } = null!;
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }

    /// <summary>21st of the month following the period (FIRS filing deadline).</summary>
    public DateOnly DueDate { get; private set; }

    public VatScheduleStatus Status { get; private set; } = VatScheduleStatus.Generated;
    public DateTimeOffset? FiledAt { get; private set; }

    // Denormalised output VAT totals (updated when items are added)
    public int TotalInvoiceCount { get; private set; }
    public decimal TotalTaxableAmount { get; private set; }
    public decimal TotalVatAmount { get; private set; }

    // Denormalised input VAT totals (updated when received invoices are processed)
    public int TotalInputInvoiceCount { get; private set; }
    public decimal TotalInputTaxableAmount { get; private set; }
    public decimal TotalInputVatAmount { get; private set; }

    public decimal NetVatPayable => TotalVatAmount - TotalInputVatAmount;

    // Navigation
    public Guid BusinessId { get; private set; }
    public Business Business { get; private set; } = null!;

    private readonly List<VatScheduleItem> _items = [];
    public IReadOnlyCollection<VatScheduleItem> Items => _items.AsReadOnly();

    private readonly List<InputVatScheduleItem> _inputItems = [];
    public IReadOnlyCollection<InputVatScheduleItem> InputItems => _inputItems.AsReadOnly();

    // ── EF constructor ───────────────────────────────────────────────────────
    private VatSchedule() { }

    // ── Static factory ───────────────────────────────────────────────────────
    public static VatSchedule Create(Guid businessId, int year, int month)
    {
        var monthName = new DateTime(year, month, 1).ToString("MMMM");
        var periodEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var dueYear = month == 12 ? year + 1 : year;
        var dueMon = month == 12 ? 1 : month + 1;

        return new VatSchedule
        {
            BusinessId = businessId,
            Year = year,
            Month = month,
            MonthName = monthName,
            PeriodStart = new DateOnly(year, month, 1),
            PeriodEnd = periodEnd,
            DueDate = new DateOnly(dueYear, dueMon, 21),
            Status = VatScheduleStatus.Generated,
        };
    }

    // ── Domain behaviour ─────────────────────────────────────────────────────
    public void AddItems(IEnumerable<VatScheduleItem> items)
    {
        foreach (var item in items)
            _items.Add(item);

        TotalInvoiceCount = _items.Count;
        TotalTaxableAmount = _items.Sum(i => i.TaxableAmount);
        TotalVatAmount = _items.Sum(i => i.VatAmount);
    }

    public void AddInputItems(IEnumerable<InputVatScheduleItem> items)
    {
        foreach (var item in items)
            _inputItems.Add(item);

        TotalInputInvoiceCount = _inputItems.Count;
        TotalInputTaxableAmount = _inputItems.Sum(i => i.TaxableAmount);
        TotalInputVatAmount = _inputItems.Sum(i => i.VatAmount);
    }

    public void MarkAsFiled()
    {
        if (Status == VatScheduleStatus.Filed)
            throw new InvalidOperationException("Schedule is already filed.");

        Status = VatScheduleStatus.Filed;
        FiledAt = DateTimeOffset.UtcNow;
    }
}
