namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record DocumentReferenceDto
{
    public Guid Id { get; init; }
    public string Irn { get; init; } = null!;
    public DateOnly IssueDate { get; init; }
}