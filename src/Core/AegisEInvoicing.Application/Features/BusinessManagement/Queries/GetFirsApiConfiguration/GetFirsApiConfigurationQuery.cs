using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetFirsApiConfiguration;

public sealed record GetFirsApiConfigurationQuery : IRequest<GetFirsApiConfigurationResult>;
