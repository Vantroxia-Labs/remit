namespace AegisEInvoicing.Portal.API.Models.Party.Response;

/// <summary>
/// Response model for address information
/// </summary>
public class AddressResponse
{
    /// <summary>
    /// Street address
    /// </summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// City name
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province name
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Country name
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Postal or zip code (optional)
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// LGA code from FIRS/NRS (e.g. "NG-AB-ANO")
    /// </summary>
    public string? Lga { get; set; }
}

/// <summary>
/// Response model for party operations
/// </summary>
public class PartyResponse
{
    /// <summary>
    /// Unique identifier for the party
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the party (individual or business)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Phone number of the party
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the party
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Tax identification number (TIN) of the party
    /// </summary>
    public string TaxIdentificationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Address information of the party
    /// </summary>
    public AddressResponse Address { get; set; } = new();

    /// <summary>
    /// Date and time when the party was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Date and time when the party was last updated (optional)
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// User who created the party
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// User who last updated the party (optional)
    /// </summary>
    public Guid? UpdatedBy { get; set; }
}

/// <summary>
/// Summary response model for party list operations
/// </summary>
public class PartySummaryResponse
{
    /// <summary>
    /// Unique identifier for the party
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the party (individual or business)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the party
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Tax identification number (TIN) of the party
    /// </summary>
    public string TaxIdentificationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the party was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Response model for party creation
/// </summary>
public class CreatePartyResponse
{
    /// <summary>
    /// ID of the newly created party
    /// </summary>
    public Guid PartyId { get; set; }

    /// <summary>
    /// Success or error message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response model for party update
/// </summary>
public class UpdatePartyResponse
{
    /// <summary>
    /// ID of the updated party
    /// </summary>
    public Guid PartyId { get; set; }

    /// <summary>
    /// Success or error message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response model for party deletion
/// </summary>
public class DeletePartyResponse
{
    /// <summary>
    /// ID of the deleted party
    /// </summary>
    public Guid PartyId { get; set; }

    /// <summary>
    /// Success or error message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}