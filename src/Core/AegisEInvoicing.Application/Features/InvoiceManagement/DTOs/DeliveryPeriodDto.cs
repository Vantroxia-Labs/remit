namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record DeliveryPeriodDto
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
}