using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceDrafts;

public record GetInvoiceDraftsQuery : IRequest<List<InvoiceDraftDto>>;

public record InvoiceDraftDto
{
    public Guid Id { get; init; }
    public string? PartyName { get; init; }
    public DateOnly IssueDate { get; init; }
    public string DraftPayload { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
