using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Domain.Entities.BusinessManagement;

/// <summary>
/// Tracks the history of price changes for BusinessItems.
/// Price changes require approval by a ClientAdmin before becoming effective.
/// </summary>
public class BusinessItemPriceHistory : AuditableEntity
{
    public Guid BusinessItemId { get; private set; }
    public decimal OldPrice { get; private set; }
    public decimal NewPrice { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public string? Comments { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public string? ApprovalComments { get; private set; }
    public DateTimeOffset EffectiveFrom { get; private set; }

    // Navigation properties
    public BusinessItem BusinessItem { get; private set; } = null!;
    public User? Approver { get; private set; }

    private BusinessItemPriceHistory() { }

    /// <summary>
    /// Creates a new price change request (pending approval).
    /// </summary>
    public static BusinessItemPriceHistory Create(
        Guid businessItemId,
        decimal oldPrice,
        decimal newPrice,
        string? comments = null)
    {
        if (newPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice), "New price cannot be negative.");

        if (oldPrice == newPrice)
            throw new ArgumentException("New price must be different from old price.", nameof(newPrice));

        return new BusinessItemPriceHistory
        {
            BusinessItemId = businessItemId,
            OldPrice = oldPrice,
            NewPrice = newPrice,
            Status = ApprovalStatus.Pending,
            Comments = comments,
            EffectiveFrom = DateTimeOffset.MaxValue // Not effective until approved
        };
    }

    /// <summary>
    /// Approves the price change and sets the effective date.
    /// </summary>
    public void Approve(Guid approverId, string? comments = null)
    {
        if (Status != ApprovalStatus.Pending)
            throw new InvalidOperationException("Only pending price changes can be approved.");

        Status = ApprovalStatus.Approved;
        ApprovedBy = approverId;
        ApprovedAt = DateTimeOffset.UtcNow;
        ApprovalComments = comments;
        EffectiveFrom = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Rejects the price change.
    /// </summary>
    public void Reject(Guid approverId, string? comments = null)
    {
        if (Status != ApprovalStatus.Pending)
            throw new InvalidOperationException("Only pending price changes can be rejected.");

        Status = ApprovalStatus.Rejected;
        ApprovedBy = approverId;
        ApprovedAt = DateTimeOffset.UtcNow;
        ApprovalComments = comments;
    }

    /// <summary>
    /// Checks if this price change is currently effective (approved and effective date has passed).
    /// </summary>
    public bool IsEffective => Status == ApprovalStatus.Approved && EffectiveFrom <= DateTimeOffset.UtcNow;
}
