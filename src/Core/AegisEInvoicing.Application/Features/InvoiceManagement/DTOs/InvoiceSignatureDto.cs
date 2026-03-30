namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record InvoiceSignatureDto
{
    public string Id { get; init; } = null!;
    public string SignatoryName { get; init; } = null!;
    public string? SignatoryPosition { get; init; }
}