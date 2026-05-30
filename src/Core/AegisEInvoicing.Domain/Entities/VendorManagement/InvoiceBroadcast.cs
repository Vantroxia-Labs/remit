using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Domain.Entities.VendorManagement;

public class InvoiceBroadcast : AuditableAggregateRoot
{
    public string Title { get; private set; } = null!;
    public string InvoiceTypeCode { get; private set; } = null!;
    public DateOnly DueDate { get; private set; }
    public bool RequiresApproval { get; private set; }
    public bool IsApprovalLocked { get; private set; }
    public BroadcastStatus Status { get; private set; } = BroadcastStatus.Active;
    public string? Note { get; private set; }
    public string Currency { get; private set; } = null!;
    public Guid BusinessId { get; private set; }

    public Business Business { get; private set; } = null!;

    private readonly List<InvoiceBroadcastVendor> _broadcastVendors = [];
    public IReadOnlyCollection<InvoiceBroadcastVendor> BroadcastVendors => _broadcastVendors.AsReadOnly();

    private InvoiceBroadcast() { }

    public static InvoiceBroadcast Create(
        string title,
        string invoiceTypeCode,
        DateOnly dueDate,
        bool requiresApproval,
        string currency,
        Guid businessId,
        string? note = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        if (string.IsNullOrWhiteSpace(invoiceTypeCode))
            throw new ArgumentException("Invoice type code is required", nameof(invoiceTypeCode));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));

        if (businessId == Guid.Empty)
            throw new ArgumentException("Business ID is required", nameof(businessId));

        if (dueDate <= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Due date must be in the future", nameof(dueDate));

        return new InvoiceBroadcast
        {
            Title = title.Trim(),
            InvoiceTypeCode = invoiceTypeCode.Trim(),
            DueDate = dueDate,
            RequiresApproval = requiresApproval,
            IsApprovalLocked = false,
            Currency = currency.Trim().ToUpperInvariant(),
            BusinessId = businessId,
            Note = note?.Trim(),
            Status = BroadcastStatus.Active
        };
    }

    public void Update(string title, DateOnly dueDate, string? note)
    {
        if (Status == BroadcastStatus.Deactivated)
            throw new InvalidOperationException("Cannot edit a deactivated broadcast");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        if (dueDate <= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Due date must be in the future", nameof(dueDate));

        Title = title.Trim();
        DueDate = dueDate;
        Note = note?.Trim();
    }

    public void ExtendDueDate(DateOnly newDueDate)
    {
        if (Status == BroadcastStatus.Deactivated)
            throw new InvalidOperationException("Cannot extend due date on a deactivated broadcast");

        if (newDueDate <= DueDate)
            throw new ArgumentException("New due date must be later than the current due date", nameof(newDueDate));

        DueDate = newDueDate;
    }

    public void LockApprovalSetting()
    {
        IsApprovalLocked = true;
    }

    public void SetRequiresApproval(bool requiresApproval)
    {
        if (IsApprovalLocked)
            throw new InvalidOperationException(
                "Approval setting cannot be changed after an invoice has been submitted to NRS on this broadcast");

        RequiresApproval = requiresApproval;
    }

    public void Deactivate()
    {
        if (Status == BroadcastStatus.Deactivated)
            throw new InvalidOperationException("Broadcast is already deactivated");

        Status = BroadcastStatus.Deactivated;
    }

    public bool IsExpired() => DateOnly.FromDateTime(DateTime.UtcNow) > DueDate;
}
