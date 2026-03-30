using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoiceItem;

public record CreateInvoiceItemResult
{
    public bool Success { get; init; }
    public Guid? InvoiceItemId { get; init; }
    public string Message { get; init; } = null!;    
}
