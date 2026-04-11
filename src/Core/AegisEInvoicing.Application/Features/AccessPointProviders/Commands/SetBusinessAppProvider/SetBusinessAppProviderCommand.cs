using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.SetBusinessAppProvider;

/// <summary>
/// Sets the active Access Point Provider for a business.
/// Pass null or empty <paramref name="ProviderCode"/> to reset to the platform default (Interswitch).
/// </summary>
public record SetBusinessAppProviderCommand(
    Guid BusinessId,
    string? ProviderCode
) : IRequest<SetBusinessAppProviderResult>, ITransactionalCommand;
