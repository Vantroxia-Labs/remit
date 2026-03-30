using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.Miscellenous;

public sealed class TestSendDataRequest
{
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    [Required]
    public string Data { get; set; } = "{}";
}