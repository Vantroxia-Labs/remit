namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record AddressDto
{
    public string Street { get; init; } = null!;
    public string City { get; init; } = null!;
    public string State { get; init; } = null!;
    public string PostalCode { get; init; } = null!;
    public string Country { get; init; } = null!;
    public string? Lga { get; init; }
}
