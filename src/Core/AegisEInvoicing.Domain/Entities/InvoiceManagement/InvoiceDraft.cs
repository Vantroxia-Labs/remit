using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

/// <summary>
/// Represents a saved invoice draft that can be resumed and submitted later.
/// Stores the full form payload as JSON — no IRN or QR code is generated until submission.
/// </summary>
public class InvoiceDraft : AuditableEntity
{
    public Guid BusinessId { get; private set; }

    /// <summary>Denormalized party name for list display — kept in sync when saving.</summary>
    public string? PartyName { get; private set; }

    public DateOnly IssueDate { get; private set; }

    /// <summary>Full serialized form payload (JSON). Deserialized by the frontend on resume.</summary>
    public string DraftPayload { get; private set; } = null!;

    private InvoiceDraft() { } // EF constructor

    public static InvoiceDraft Create(
        Guid businessId,
        Guid createdBy,
        DateOnly issueDate,
        string draftPayload,
        string? partyName = null)
    {
        return new InvoiceDraft
        {
            BusinessId = businessId,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            IssueDate = issueDate,
            DraftPayload = draftPayload,
            PartyName = partyName,
            IsDeleted = false
        };
    }

    public void Update(DateOnly issueDate, string draftPayload, string? partyName)
    {
        IssueDate = issueDate;
        DraftPayload = draftPayload;
        PartyName = partyName;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
