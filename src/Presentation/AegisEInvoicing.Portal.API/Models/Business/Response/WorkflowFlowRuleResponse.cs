namespace AegisEInvoicing.Portal.API.Models.Business.Response;

/// <summary>
/// Response model for workflow-based FlowRule operations
/// </summary>
public class WorkflowFlowRuleResponse
{
    /// <summary>
    /// FlowRule ID
    /// </summary>
    public Guid FlowRuleId { get; set; }

    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Workflow configuration that was created/updated
    /// </summary>
    public WorkflowConfigurationResponse? WorkflowConfiguration { get; set; }
}

/// <summary>
/// Response model for workflow configuration
/// </summary>
public class WorkflowConfigurationResponse
{
    /// <summary>
    /// Workflow name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Workflow description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The initial action that starts the workflow
    /// </summary>
    public string InitialAction { get; set; } = string.Empty;

    /// <summary>
    /// Workflow states configuration
    /// </summary>
    public Dictionary<string, WorkflowStateResponse> States { get; set; } = new();
}

/// <summary>
/// Response model for workflow state
/// </summary>
public class WorkflowStateResponse
{
    /// <summary>
    /// The action that represents this state
    /// </summary>
    public string StateAction { get; set; } = string.Empty;

    /// <summary>
    /// Actions available when in this state
    /// </summary>
    public List<string> AvailableActions { get; set; } = new();

    /// <summary>
    /// Roles allowed to perform actions in this state
    /// </summary>
    public List<string> AllowedRoles { get; set; } = new();

    /// <summary>
    /// Whether this is a final state in the workflow
    /// </summary>
    public bool IsFinalState { get; set; }

    /// <summary>
    /// Description of what this state represents
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Next step to execute when this state is completed
    /// </summary>
    public string? NextStep { get; set; }
}

/// <summary>
/// Response model for getting available workflow actions
/// </summary>
public class AvailableWorkflowActionsResponse
{
    /// <summary>
    /// Invoice ID
    /// </summary>
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// Current workflow state
    /// </summary>
    public string? CurrentState { get; set; }

    /// <summary>
    /// Available actions for the current user
    /// </summary>
    public List<string> AvailableActions { get; set; } = new();

    /// <summary>
    /// Workflow configuration name
    /// </summary>
    public string? WorkflowName { get; set; }

    /// <summary>
    /// Whether the workflow is in a final state
    /// </summary>
    public bool IsInFinalState { get; set; }
}
