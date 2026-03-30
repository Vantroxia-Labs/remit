namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record TaxSubtotalDto
{
    public double TaxableAmount { get; init; }
    public double TaxAmount { get; init; }
    public TaxCategoryDto TaxCategory { get; init; } = null!;
}

public record TaxCategoryDto
{
    public string ID { get; init; } = null!;
    public double Percent { get; init; }
}