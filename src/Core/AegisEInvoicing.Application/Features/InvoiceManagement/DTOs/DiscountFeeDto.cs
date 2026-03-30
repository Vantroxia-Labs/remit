using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record DiscountFeeDto
{
    public decimal Amount { get; init; }
    public FeeStandardUnit Code { get; init; }
}