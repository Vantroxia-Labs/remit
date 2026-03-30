namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record InvoiceLinePriceDto
{
    public double PriceAmount { get; init; }
    public int BaseQuantity { get; init; }
    public string PriceUnit { get; init; } = null!;
}
