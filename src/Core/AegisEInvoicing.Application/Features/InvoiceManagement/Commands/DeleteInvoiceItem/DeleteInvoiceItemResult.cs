namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.DeleteInvoiceItem;

public record DeleteInvoiceItemResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
}
