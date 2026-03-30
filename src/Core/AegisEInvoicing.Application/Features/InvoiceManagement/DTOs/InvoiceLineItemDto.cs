namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record InvoiceLineItemDto
{
    public string Description { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? SellersItemIdentification { get; init; }
}
