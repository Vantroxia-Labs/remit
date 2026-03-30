namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoiceItem;

public record UpdateInvoiceItemResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
}
