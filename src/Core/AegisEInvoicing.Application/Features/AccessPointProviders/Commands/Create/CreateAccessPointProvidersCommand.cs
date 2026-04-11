using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Create;

/// <summary>
/// Creates a new APP provider configuration. AegisAdmin only.
/// Credentials are supplied as plaintext JSON; the handler encrypts them before persistence.
/// The JSON schema is vendor-specific and opaque to the application layer.
/// </summary>
public record CreateAccessPointProvidersCommand(
    string Name,
    string? Description,
    AppVendor Vendor,
    string BaseUrl,
    string? CredentialsJson,
    string? SandboxBaseUrl,
    string? SandboxCredentialsJson
) : IRequest<CreateAccessPointProvidersResult>, ITransactionalCommand;
