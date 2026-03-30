namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record AllowanceChargeDto
{
    public bool ChargeIndicator { get; init; }
    public double Amount { get; init; }
}