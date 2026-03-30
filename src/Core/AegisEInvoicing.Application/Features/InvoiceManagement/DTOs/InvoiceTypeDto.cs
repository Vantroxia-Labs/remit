namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record InvoiceTypeDto
{
    public string Name { get; init; } = null!;
    public int Code { get; init; }
}