using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetServiceCodes;

public sealed record GetServiceCodesResponse : GenericResponse
{
    [JsonPropertyName("data")]
    public List<ServiceCode> Data { get; set; } = [];
}

public sealed record ServiceCode
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = null!;
}