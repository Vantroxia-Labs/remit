namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record TaxTotalDto
{
    public double TaxAmount { get; init; }
    public List<TaxSubtotalDto> TaxSubtotal { get; init; } = [];
}