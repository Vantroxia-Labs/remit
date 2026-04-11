using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.SetBusinessEnvironmentMode;

/// <summary>
/// Switches a business between Sandbox and Production credential sets for its active APP provider.
/// </summary>
public record SetBusinessEnvironmentModeCommand(
    Guid BusinessId,
    AppEnvironmentMode EnvironmentMode
) : IRequest<SetBusinessEnvironmentModeResult>, ITransactionalCommand;
