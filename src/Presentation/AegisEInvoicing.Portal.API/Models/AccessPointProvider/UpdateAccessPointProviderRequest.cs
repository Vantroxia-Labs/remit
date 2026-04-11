using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.AccessPointProvider;

public class UpdateAccessPointProviderRequest
{
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? ApiKeyHeaderName { get; set; }
    public string? SignatureHeaderName { get; set; }

    // Sandbox
    [Required]
    public string SandboxBaseUrl { get; set; } = string.Empty;
    public string? SandboxApiKey { get; set; }
    public string? SandboxApiSecret { get; set; }
    public string? SandboxTokenEndpoint { get; set; }

    // Production
    [Required]
    public string ProductionBaseUrl { get; set; } = string.Empty;
    public string? ProductionApiKey { get; set; }
    public string? ProductionApiSecret { get; set; }
    public string? ProductionTokenEndpoint { get; set; }
}
