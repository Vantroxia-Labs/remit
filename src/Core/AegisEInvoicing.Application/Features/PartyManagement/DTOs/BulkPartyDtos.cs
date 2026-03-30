namespace AegisEInvoicing.Application.Features.PartyManagement.DTOs;

public class BulkPartyDtos
{
    public List<CreateBulkPartyRequest> Parties { get; set; } = new List<CreateBulkPartyRequest>();
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int totalSuccessfulUploadedCount { get; set; }
    public int totalErrorsUploadedCount { get; set; }
}

public class CreateBulkPartyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TaxIdentificationNumber { get; set; } = string.Empty;
    public CreateBulkPartyAddressRequest Address { get; set; } = new();
}

public class CreateBulkPartyAddressRequest
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}

public record BulkPartyResult(bool IsSuccess, string Message);
