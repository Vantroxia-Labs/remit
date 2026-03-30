namespace AegisEInvoicing.Application.Features.BusinessManagement.DTOs;

/// <summary>
/// Simplified response DTO for flowrule details endpoint
/// Contains essential information about the flow rule and its range-based configuration
/// </summary>
public record FlowRuleDetailsResponseDto
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
}
