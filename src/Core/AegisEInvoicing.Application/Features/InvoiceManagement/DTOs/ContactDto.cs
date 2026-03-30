namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record ContactDto
{
    public string? Telephone { get; init; }
    public string EmailAddress { get; init; } = null!;   
}