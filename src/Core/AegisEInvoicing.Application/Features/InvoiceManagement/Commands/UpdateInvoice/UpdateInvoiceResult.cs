using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoice;

public record UpdateInvoiceResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
}
