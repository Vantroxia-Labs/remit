using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record AdditionalFeeDto
{
    public decimal Amount { get; init; }
    public FeeStandardUnit Code { get; init; }
}