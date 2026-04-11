using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;

/// <summary>
/// Read model for an APP provider configuration.
/// Credentials are never returned — they are encrypted at rest and have no use on the client.
/// The frontend shows presence indicators (HasProductionCredentials / HasSandboxCredentials)
/// so the admin knows whether credentials have been configured.
/// </summary>
public record AccessPointProvidersDto(
    Guid Id,
    string Name,
    string? Description,
    AppVendor Vendor,
    string BaseUrl,
    bool HasProductionCredentials,
    string? SandboxBaseUrl,
    bool HasSandboxCredentials,
    bool IsActive,
    DateTimeOffset CreatedAt);
