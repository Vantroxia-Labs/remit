namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record PaymentMeansDto
{
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
}