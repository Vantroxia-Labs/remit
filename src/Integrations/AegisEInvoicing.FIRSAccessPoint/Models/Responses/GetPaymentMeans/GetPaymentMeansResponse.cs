using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetPaymentMeans;

public sealed record GetPaymentMeansResponse : GenericResponse
{
    [JsonPropertyName("data")]
    public List<PaymentMeans> Data { get; set; } = new();
}

public sealed record PaymentMeans
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;
}