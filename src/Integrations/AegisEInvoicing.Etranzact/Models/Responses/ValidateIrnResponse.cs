using System.Text.Json.Serialization;
using AegisEInvoicing.Etranzact.Models;

namespace AegisEInvoicing.Etranzact.Models.Responses;

/// <summary>
/// Response from the validate IRN endpoint.
/// Equivalent to Interswitch's LookupWithIRN response.
/// </summary>
public sealed class ValidateIrnResponse : EtranzactResponse<ValidateIrnData>
{
}

public sealed class ValidateIrnData
{
    [JsonPropertyName("irn")]
    public string? Irn { get; set; }
}
