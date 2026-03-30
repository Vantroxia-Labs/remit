using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Update;

public record UpdateAccessPointProvidersCommand(Guid configurationId, string name, string description, string env, string baseUrl, string apiKey, string apiSecret) : IRequest<UpdateAccessPointProvidersResult>, ITransactionalCommand;
