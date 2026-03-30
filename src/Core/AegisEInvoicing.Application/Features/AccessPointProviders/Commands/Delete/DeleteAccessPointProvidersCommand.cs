using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Delete;

public record DeleteAccessPointProvidersCommand(Guid configurationId) : IRequest<DeleteAccessPointProvidersResult>, ITransactionalCommand;
