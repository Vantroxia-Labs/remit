using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.SetBusinessAppProvider;

/// <summary>
/// Sets the active APP vendor for a business.
/// Pass null <paramref name="Vendor"/> to reset to the platform default (Interswitch).
/// </summary>
public record SetBusinessAppProviderCommand(
    Guid BusinessId,
    AppVendor? Vendor
) : IRequest<SetBusinessAppProviderResult>, ITransactionalCommand;
