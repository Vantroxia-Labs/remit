using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetAllLocalGovernments;

public sealed record GetAllLocalGovernmentsResponse : GenericResponse
{
    public List<LocalGovernment> Data { get; set; } = new();
}

public sealed record LocalGovernment
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("state_code")]
    public string StateCode { get; set; } = null!;
}