using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.AccessPointProvider;

public class AccessPointProviderRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Lowercase stable key matching IAccessPointProviderClient.ProviderCode.
    /// e.g. "interswitch", "digitax", "etranzact", "bluebridge".
    /// </summary>
    [Required]
    public string AdapterKey { get; set; } = string.Empty;

    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Plaintext JSON of production credentials. Shape is adapter-specific.
    /// The API encrypts this before persistence. Omit to leave existing credentials unchanged.
    /// </summary>
    public string? CredentialsJson { get; set; }

    public string? SandboxBaseUrl { get; set; }

    /// <summary>
    /// Plaintext JSON of sandbox credentials. Omit if this adapter has no sandbox.
    /// </summary>
    public string? SandboxCredentialsJson { get; set; }
}
