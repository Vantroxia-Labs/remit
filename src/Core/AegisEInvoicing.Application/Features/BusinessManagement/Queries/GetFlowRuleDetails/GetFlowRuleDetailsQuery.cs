using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetFlowRuleDetails;

public record GetFlowRuleDetailsQuery(Guid? FlowRuleId = null) : IRequest<IEnumerable<FlowRuleDetailsResponseDto>>;
