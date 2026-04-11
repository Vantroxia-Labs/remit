using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Update;

/// <summary>
/// Updates display details and, optionally, credentials for an APP provider configuration.
/// AegisAdmin only. Pass null for credential arguments to keep existing encrypted values.
/// Vendor cannot be changed — create a new configuration instead.
/// </summary>
public record UpdateAccessPointProvidersCommand(
    Guid ConfigurationId,
    string Name,
    string? Description,
    string? BaseUrl,
    string? CredentialsJson,
    string? SandboxBaseUrl,
    string? SandboxCredentialsJson
) : IRequest<UpdateAccessPointProvidersResult>, ITransactionalCommand;
