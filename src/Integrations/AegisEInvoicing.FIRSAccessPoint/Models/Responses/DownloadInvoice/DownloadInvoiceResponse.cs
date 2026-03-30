using System.Text.Json.Serialization;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.DownloadInvoice;

public sealed record DownloadInvoiceResponse : GenericResponse
{
    [JsonPropertyName("data")]
    public DownloadInvoiceResponseData? Data { get; set; }
}

public sealed record DownloadInvoiceResponseData
{
    [JsonPropertyName("iv_hex")]
    public string IvHex { get; set; } = null!;
    [JsonPropertyName("pub")]
    public string Pub { get; set; } = null!;
    [JsonPropertyName("data")]
    public string Data { get; set; } = null!;
}