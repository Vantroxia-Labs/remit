using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Create;

public record CreateAccessPointProvidersCommand(string Name, string Description, string Environment, string BaseUrl, string ApiKey, string ApiSecret) : IRequest<CreateAccessPointProvidersResult>, ITransactionalCommand;
