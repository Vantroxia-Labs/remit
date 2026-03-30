using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetQrCodeConfiguration;

public sealed record GetQrCodeConfigurationQuery : IRequest<GetQrCodeConfigurationResult>;