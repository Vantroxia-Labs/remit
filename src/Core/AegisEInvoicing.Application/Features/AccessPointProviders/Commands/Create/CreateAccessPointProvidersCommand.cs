using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Create;

/// <summary>
/// Creates a new <c>AppProviderConfiguration</c> record (AegisAdmin only).
/// Credentials are accepted in plaintext and encrypted before persistence.
/// </summary>
public record CreateAccessPointProvidersCommand(
    string ProviderCode,
    string DisplayName,
    string Description,
    AppAuthScheme AuthScheme,
    string? ApiKeyHeaderName,
    string? SignatureHeaderName,
    // Sandbox
    string SandboxBaseUrl,
    string? SandboxApiKey,
    string? SandboxApiSecret,
    string? SandboxTokenEndpoint,
    // Production
    string ProductionBaseUrl,
    string? ProductionApiKey,
    string? ProductionApiSecret,
    string? ProductionTokenEndpoint
) : IRequest<CreateAccessPointProvidersResult>, ITransactionalCommand;
