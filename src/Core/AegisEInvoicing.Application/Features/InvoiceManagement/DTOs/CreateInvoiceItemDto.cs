namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record CreateInvoiceItemDto
{
    public Guid BusinessItemId { get; init; }
    public decimal Quantity { get; init; }
    public DiscountFeeDto? DiscountFee { get; init; }
    public AdditionalFeeDto? AdditionalFee { get; init; }
}
