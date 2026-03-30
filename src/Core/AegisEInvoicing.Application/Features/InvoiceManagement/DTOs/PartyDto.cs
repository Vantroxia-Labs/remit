using AegisEInvoicing.Domain.ValueObjects;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record PartyDto
{
    public string Name { get; init; } = null!;
    public TIN Tin { get; init; } = null!;    
    public string Email { get; init; } = null!;
    public string Phone { get; init; } = null!;
    public Address Address { get; init; } = null!;
}
