namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.SoftDeleteBusinessFlowRule;

public class SoftDeleteBusinessFlowRuleResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? FlowRuleId { get; set; }
}
