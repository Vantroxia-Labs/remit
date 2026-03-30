namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.DeleteInvoice;

public record DeleteInvoiceResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
}
