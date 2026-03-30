namespace AegisEInvoicing.Portal.API.Models.Business.Response;

public class UpdateFlowRuleResponse
{
    public Guid FlowRuleId { get; set; }
    public string Message { get; set; } = string.Empty;
}