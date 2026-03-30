using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.SignInvoice;

public sealed record SignInvoiceResponse : GenericResponse
{
    [JsonPropertyName("data")]
    public Data Data { get; set; } = null!;
}

public class Data
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }
}
