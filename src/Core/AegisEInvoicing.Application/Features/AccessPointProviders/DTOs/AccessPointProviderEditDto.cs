namespace AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;

/// <summary>
/// Full edit model returned only to AegisAdmin for the edit form.
/// Decrypted credential JSON is included so the form can be pre-populated.
/// </summary>
public record AccessPointProviderEditDto(
    Guid Id,
    string Name,
    string? Description,
    string AdapterKey,
    string DisplayName,
    string BaseUrl,
    string? CredentialsJson,
    string? SandboxBaseUrl,
    string? SandboxCredentialsJson,
    bool IsActive,
    DateTimeOffset CreatedAt);
