using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.SoftDeleteBusinessFlowRule;

public record SoftDeleteBusinessFlowRuleCommand(
    Guid FlowRuleId
) : IRequest<SoftDeleteBusinessFlowRuleResult>;
