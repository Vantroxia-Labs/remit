namespace AegisEInvoicing.Application.Features.BusinessManagement.DTOs;

/// <summary>
/// DTO for FlowRule with range-based amount matching and simplified approval workflow
/// </summary>
public record FlowRuleDto
{
    /// <summary>
    /// Unique identifier of the flow rule
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Name of the flow rule
    /// </summary>
    public string Name { get; init; } = default!;

    /// <summary>
    /// Description of the flow rule
    /// </summary>
    public string Description { get; init; } = default!;

    /// <summary>
    /// Minimum invoice amount for this flow rule (inclusive)
    /// </summary>
    public decimal MinAmount { get; init; }

    /// <summary>
    /// Maximum invoice amount for this flow rule (inclusive)
    /// </summary>
    public decimal MaxAmount { get; init; }

    /// <summary>
    /// Indicates whether this flow rule requires ClientAdmin approval
    /// If false, invoices matching this rule are auto-approved
    /// If true, invoices matching this rule are set to PENDING_APPROVAL status
    /// </summary>
    public bool RequiresClientAdminApproval { get; init; }

    /// <summary>
    /// Priority for rule matching when multiple rules could apply (lower number = higher priority)
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// Enables time-based filtering for this flow rule (disabled by default)
    /// </summary>
    public bool EnableTimeBasedRules { get; init; }

    /// <summary>
    /// Start time for active period (e.g., 09:00). Only applies if EnableTimeBasedRules is true
    /// </summary>
    public TimeSpan? ActiveStartTime { get; init; }

    /// <summary>
    /// End time for active period (e.g., 17:00). Only applies if EnableTimeBasedRules is true
    /// </summary>
    public TimeSpan? ActiveEndTime { get; init; }

    /// <summary>
    /// Days of week when this rule is active (e.g., Monday-Friday). Only applies if EnableTimeBasedRules is true
    /// </summary>
    public DayOfWeek[]? ActiveDaysOfWeek { get; init; }

    /// <summary>
    /// Timestamp when the flow rule was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Timestamp when the flow rule was last updated
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Descriptive text explaining the workflow this rule represents
    /// </summary>
    public string WorkflowDescription =>
        RequiresClientAdminApproval
            ? $"Invoices from {MinAmount:N2} to {MaxAmount:N2} require ClientAdmin approval"
            : $"Invoices from {MinAmount:N2} to {MaxAmount:N2} are auto-approved";
}
