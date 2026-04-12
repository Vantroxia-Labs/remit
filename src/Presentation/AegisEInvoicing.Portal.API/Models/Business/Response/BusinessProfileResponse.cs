using System.Text.Json.Serialization;

namespace AegisEInvoicing.Portal.API.Models.Business.Response;

/// <summary>
/// Business profile response shaped to match the frontend BusinessProfile interface.
/// </summary>
public class BusinessProfileResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BusinessRegistrationNumber { get; set; } = string.Empty;
    public string TaxIdentificationNumber { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>Serialises as "NRSBusinessId" to match the frontend property name.</summary>
    [JsonPropertyName("NRSBusinessId")]
    public string? NRSBusinessId { get; set; }

    public bool IsActive { get; set; }
    public BusinessAddressResponse? RegisteredAddress { get; set; }
    public bool OnboardingCompleted { get; set; }
}

public class BusinessAddressResponse
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}
