using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.AccessPointProvider;

public class UpdateAccessPointProviderRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Updated production base URL. Omit to keep the existing value.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Plaintext JSON of updated production credentials. Omit to keep existing encrypted credentials.
    /// </summary>
    public string? CredentialsJson { get; set; }

    public string? SandboxBaseUrl { get; set; }

    /// <summary>
    /// Plaintext JSON of updated sandbox credentials. Omit to keep existing encrypted credentials.
    /// </summary>
    public string? SandboxCredentialsJson { get; set; }
}
