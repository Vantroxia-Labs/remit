namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record CurrencyDto
{
    public string Name { get; init; } = null!;
    public string Code { get; init; } = null!;
}