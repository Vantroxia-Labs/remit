using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

/// <summary>
/// Monthly WHT (Withholding Tax) schedule for a business.
/// One schedule is generated per calendar month; it captures all received invoices
/// for which WHT was deducted during that period.
/// Filing deadline is the 21st of the following month (NRS) or 30th (State IRS).
/// </summary>
public class WhtSchedule : AuditableAggregateRoot
{
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string MonthName { get; private set; } = null!;
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }

    /// <summary>21st of the month following the period (NRS federal filing deadline).</summary>
    public DateOnly DueDate { get; private set; }

    public WhtScheduleStatus Status { get; private set; } = WhtScheduleStatus.Generated;
    public DateTimeOffset? FiledAt { get; private set; }

    // Denormalised totals — updated when items are added
    public int TotalItemCount { get; private set; }
    public decimal TotalGrossAmount { get; private set; }
    public decimal TotalWhtAmount { get; private set; }

    // Split by authority for reporting
    public decimal TotalNrsWhtAmount { get; private set; }
    public decimal TotalStateWhtAmount { get; private set; }

    // Navigation
    public Guid BusinessId { get; private set; }
    public Business Business { get; private set; } = null!;

    private readonly List<WhtScheduleItem> _items = [];
    public IReadOnlyCollection<WhtScheduleItem> Items => _items.AsReadOnly();

    // ── EF constructor ───────────────────────────────────────────────────────
    private WhtSchedule() { }

    // ── Static factory ───────────────────────────────────────────────────────
    public static WhtSchedule Create(Guid businessId, int year, int month)
    {
        var monthName = new DateTime(year, month, 1).ToString("MMMM");
        var periodEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var dueYear = month == 12 ? year + 1 : year;
        var dueMon = month == 12 ? 1 : month + 1;

        return new WhtSchedule
        {
            BusinessId = businessId,
            Year = year,
            Month = month,
            MonthName = monthName,
            PeriodStart = new DateOnly(year, month, 1),
            PeriodEnd = periodEnd,
            DueDate = new DateOnly(dueYear, dueMon, 21),
            Status = WhtScheduleStatus.Generated,
        };
    }

    // ── Domain behaviour ─────────────────────────────────────────────────────
    public void AddItems(IEnumerable<WhtScheduleItem> items)
    {
        foreach (var item in items)
            _items.Add(item);

        TotalItemCount = _items.Count;
        TotalGrossAmount = _items.Sum(i => i.GrossAmount);
        TotalWhtAmount = _items.Sum(i => i.WhtAmount);
        TotalNrsWhtAmount = _items.Where(i => i.TaxAuthority == WhtTaxAuthority.NRS).Sum(i => i.WhtAmount);
        TotalStateWhtAmount = _items.Where(i => i.TaxAuthority == WhtTaxAuthority.StateIRS).Sum(i => i.WhtAmount);
    }

    public void MarkAsFiled()
    {
        if (Status == WhtScheduleStatus.Filed)
            throw new InvalidOperationException("WHT schedule is already filed.");

        Status = WhtScheduleStatus.Filed;
        FiledAt = DateTimeOffset.UtcNow;
    }
}
