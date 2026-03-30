namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record LegalMonetaryTotalDto
{
    public double LineExtensionAmount { get; init; }
    public double TaxExclusiveAmount { get; init; }
    public double TaxInclusiveAmount { get; init; }
    public double PayableAmount { get; init; }
}