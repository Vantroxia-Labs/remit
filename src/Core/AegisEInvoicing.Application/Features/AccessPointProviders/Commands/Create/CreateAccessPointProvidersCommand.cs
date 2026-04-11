using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Create;

/// <summary>
/// Creates a new APP provider configuration. AegisAdmin only.
/// Credentials are supplied as plaintext JSON; the handler encrypts them before persistence.
/// The JSON schema is adapter-specific and opaque to the application layer.
/// </summary>
public record CreateAccessPointProvidersCommand(
    string Name,
    string? Description,
    string AdapterKey,
    string BaseUrl,
    string? CredentialsJson,
    string? SandboxBaseUrl,
    string? SandboxCredentialsJson
) : IRequest<CreateAccessPointProvidersResult>, ITransactionalCommand;
