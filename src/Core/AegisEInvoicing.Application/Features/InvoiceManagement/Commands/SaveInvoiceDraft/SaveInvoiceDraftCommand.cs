using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SaveInvoiceDraft;

/// <summary>
/// Creates or updates a portal invoice draft.
/// If DraftId is provided the existing draft is updated; otherwise a new one is created.
/// </summary>
public record SaveInvoiceDraftCommand : IRequest<SaveInvoiceDraftResult>
{
    public Guid? DraftId { get; init; }

    /// <summary>
    /// Full form state serialized as a JSON string.
    /// The frontend is responsible for serialization/deserialization.
    /// </summary>
    public string DraftPayload { get; init; } = null!;

    /// <summary>Denormalized party name for list display.</summary>
    public string? PartyName { get; init; }

    public DateOnly IssueDate { get; init; }
}
