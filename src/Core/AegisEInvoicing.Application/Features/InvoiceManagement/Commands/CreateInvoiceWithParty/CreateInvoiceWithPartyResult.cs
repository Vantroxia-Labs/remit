namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoiceWithParty;

public record CreateInvoiceWithPartyResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}