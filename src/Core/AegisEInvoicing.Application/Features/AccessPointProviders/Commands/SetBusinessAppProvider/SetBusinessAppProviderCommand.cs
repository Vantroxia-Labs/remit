using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.SetBusinessAppProvider;

/// <summary>
/// Sets the active APP adapter for a business.
/// Pass null <paramref name="AdapterKey"/> to reset to the platform default.
/// </summary>
public record SetBusinessAppProviderCommand(
    Guid BusinessId,
    string? AdapterKey
) : IRequest<SetBusinessAppProviderResult>, ITransactionalCommand;
