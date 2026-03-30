namespace AegisEInvoicing.Application.Features.PartyManagement.DTOs;

public record PartyDto(
    Guid Id,
    string Name,
    string Phone,
    string Email,
    string TaxIdentificationNumber,
    AddressDto Address,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    Guid CreatedBy,
    Guid? UpdatedBy);

public record PartyResult(
    bool IsSuccess,
    string Message,
    Guid? PartyId = null);

public record GetPartyByIdResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
    public PartyDto? Party { get; init; }
}

public record PartySummaryDto(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    string TaxIdentificationNumber,
    DateTimeOffset CreatedAt);

public record AddressDto(
    string Street,
    string City,
    string State,
    string Country,
    string? PostalCode);

public record CreateAddressDto(
    string Street,
    string City,
    string State,
    string Country,
    string? PostalCode);

public record UpdateAddressDto(
    string Street,
    string City,
    string State,
    string Country,
    string? PostalCode);

public record CreatePartyDto(
    string Name,
    string Phone,
    string Email,
    string TaxIdentificationNumber,
    string Description,
    CreateAddressDto Address);
