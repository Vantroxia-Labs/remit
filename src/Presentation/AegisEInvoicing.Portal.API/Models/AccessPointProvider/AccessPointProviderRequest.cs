using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.AccessPointProvider;

public class AccessPointProviderRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;
    [Required]
    public string Environment { get; set; } = string.Empty;
    [Required]
    public string BaseUrl { get; set; } = string.Empty;
    [Required]
    public string ApiKey { get; set; } = string.Empty;
    [Required]
    public string ApiSecret { get; set; } = string.Empty;
}
