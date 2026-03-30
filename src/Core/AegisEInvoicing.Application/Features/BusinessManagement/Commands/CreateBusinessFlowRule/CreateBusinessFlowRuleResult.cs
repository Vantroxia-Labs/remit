namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.BusinessFlowRule;

public record CreateBusinessFlowRuleResult(
    bool IsSuccess,
    string Message,
    Guid? FlowRuleId = null,
    List<string>? Errors = null);
