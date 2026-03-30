namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateFIRSInvoice;

public record CreateFIRSInvoiceResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid? InvoiceId { get; init; }
    public Guid? PartyId { get; init; }
    public string IRN { get; init; } = string.Empty;
}