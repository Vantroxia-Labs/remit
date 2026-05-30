using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetAllStates;

public sealed record GetAllStatesResponse : GenericResponse
{
    public List<FirsState> Data { get; set; } = new();
}

public sealed record FirsState
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;
}