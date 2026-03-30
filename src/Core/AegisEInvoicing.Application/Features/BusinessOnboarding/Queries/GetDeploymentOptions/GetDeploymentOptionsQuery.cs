using AegisEInvoicing.Application.Features.BusinessOnboarding.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessOnboarding.Queries.GetDeploymentOptions;

public record GetDeploymentOptionsQuery : IRequest<IEnumerable<DeploymentOptionDto>>
{
}