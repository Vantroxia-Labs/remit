using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.Business.Request;

/// <summary>
/// Request model for creating FlowRule with state-based workflow configuration
/// </summary>
public class CreateWorkflowFlowRuleRequest
{
    /// <summary>
    /// Name of the flow rule
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the flow rule
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Amount threshold that triggers this flow rule
    /// </summary>
    [Required]
    [Range(1000.01, double.MaxValue, ErrorMessage = "Amount must be greater than NGN1000")]
    public double Amount { get; set; }

    /// <summary>
    /// Workflow configuration
    /// </summary>
    [Required]
    public WorkflowConfigurationRequest Workflow { get; set; } = new();
}

/// <summary>
/// Workflow configuration request model
/// </summary>
public class WorkflowConfigurationRequest
{
    /// <summary>
    /// The initial action that starts the workflow
    /// </summary>
    [Required]
    public string InitialAction { get; set; } = string.Empty;

    /// <summary>
    /// Workflow states configuration
    /// </summary>
    [Required]
    public Dictionary<string, WorkflowStateRequest> States { get; set; } = new();
}

/// <summary>
/// Workflow state request model
/// </summary>
public class WorkflowStateRequest
{
    /// <summary>
    /// Actions available when in this state
    /// </summary>
    public List<string> AvailableActions { get; set; } = new();

    /// <summary>
    /// Roles allowed to perform actions in this state
    /// </summary>
    [Required]
    public List<string> AllowedRoles { get; set; } = new();

    /// <summary>
    /// Whether this is a final state in the workflow
    /// </summary>
    public bool IsFinalState { get; set; } = false;

    /// <summary>
    /// Description of what this state represents
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Next step to execute when this state is completed
    /// </summary>
    public string? NextStep { get; set; }
}
