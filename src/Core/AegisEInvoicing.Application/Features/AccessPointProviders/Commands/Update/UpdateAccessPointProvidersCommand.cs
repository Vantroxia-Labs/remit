using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Update;

/// <summary>
/// Updates credentials and display details for an existing <c>AppProviderConfiguration</c>.
/// AegisAdmin only. Credentials are re-encrypted on update.
/// </summary>
public record UpdateAccessPointProvidersCommand(
    Guid ConfigurationId,
    string DisplayName,
    string Description,
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
) : IRequest<UpdateAccessPointProvidersResult>, ITransactionalCommand;
