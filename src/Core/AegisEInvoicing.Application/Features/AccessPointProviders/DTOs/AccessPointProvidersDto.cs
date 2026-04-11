using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;

public record AccessPointProvidersDto(
    Guid Id,
    string ProviderCode,
    string DisplayName,
    string Description,
    AppAuthScheme AuthScheme,
    string? ApiKeyHeaderName,
    string? SignatureHeaderName,
    // Sandbox (credentials masked for non-admin callers)
    string SandboxBaseUrl,
    string SandboxApiKey,
    string SandboxApiSecret,
    string? SandboxTokenEndpoint,
    // Production
    string ProductionBaseUrl,
    string ProductionApiKey,
    string ProductionApiSecret,
    string? ProductionTokenEndpoint,
    bool IsActive,
    DateTimeOffset CreatedAt);
