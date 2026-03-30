using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Portal.API.Models.Business.Request;

/// <summary>
/// Simplified request model for creating FlowRules
/// Uses range-based amount matching and RequiresClientAdminApproval flag
/// </summary>
public class SimpleCreateFlowRuleRequest
{
    /// <summary>
    /// Name of the flow rule
    /// </summary>
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the flow rule
    /// </summary>
    [Required]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Minimum invoice amount for this flow rule (inclusive)
    /// </summary>
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Minimum amount must be 0 or greater")]
    [JsonPropertyName("minAmount")]
    public decimal MinAmount { get; set; }

    /// <summary>
    /// Maximum invoice amount for this flow rule (inclusive)
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Maximum amount must be greater than 0")]
    [JsonPropertyName("maxAmount")]
    public decimal MaxAmount { get; set; }

    /// <summary>
    /// Indicates whether this flow rule requires ClientAdmin approval
    /// If false, invoices matching this rule are auto-approved
    /// If true, invoices matching this rule will be set to PENDING_APPROVAL status
    /// </summary>
    [Required]
    [JsonPropertyName("requiresClientAdminApproval")]
    public bool RequiresClientAdminApproval { get; set; }

    /// <summary>
    /// Priority for rule matching when multiple rules could apply (lower number = higher priority)
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Priority must be 1 or greater")]
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Enables time-based filtering for this flow rule (disabled by default)
    /// </summary>
    [JsonPropertyName("enableTimeBasedRules")]
    public bool EnableTimeBasedRules { get; set; } = false;

    /// <summary>
    /// Start time for active period (e.g., 09:00). Only applies if EnableTimeBasedRules is true
    /// </summary>
    [JsonPropertyName("activeStartTime")]
    public TimeSpan? ActiveStartTime { get; set; }

    /// <summary>
    /// End time for active period (e.g., 17:00). Only applies if EnableTimeBasedRules is true
    /// </summary>
    [JsonPropertyName("activeEndTime")]
    public TimeSpan? ActiveEndTime { get; set; }

    /// <summary>
    /// Days of week when this rule is active (e.g., Monday-Friday). Only applies if EnableTimeBasedRules is true
    /// </summary>
    [JsonPropertyName("activeDaysOfWeek")]
    public DayOfWeek[]? ActiveDaysOfWeek { get; set; }

    /// <summary>
    /// Validates the flow rule request
    /// </summary>
    /// <returns>Collection of validation error messages</returns>
    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (MinAmount < 0)
            errors.Add("Minimum amount must be 0 or greater");

        if (MaxAmount <= MinAmount)
            errors.Add("Maximum amount must be greater than minimum amount");

        if (EnableTimeBasedRules)
        {
            if (ActiveStartTime.HasValue && ActiveEndTime.HasValue && ActiveEndTime <= ActiveStartTime)
                errors.Add("Active end time must be after active start time");
        }

        return errors;
    }
}

/// <summary>
/// Simplified request model for updating FlowRules
/// Uses range-based amount matching and RequiresClientAdminApproval flag
/// </summary>
public class SimpleUpdateFlowRuleRequest
{
    /// <summary>
    /// Name of the flow rule
    /// </summary>
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the flow rule
    /// </summary>
    [Required]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Minimum invoice amount for this flow rule (inclusive)
    /// </summary>
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Minimum amount must be 0 or greater")]
    [JsonPropertyName("minAmount")]
    public decimal MinAmount { get; set; }

    /// <summary>
    /// Maximum invoice amount for this flow rule (inclusive)
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Maximum amount must be greater than 0")]
    [JsonPropertyName("maxAmount")]
    public decimal MaxAmount { get; set; }

    /// <summary>
    /// Indicates whether this flow rule requires ClientAdmin approval
    /// If false, invoices matching this rule are auto-approved
    /// If true, invoices matching this rule will be set to PENDING_APPROVAL status
    /// </summary>
    [Required]
    [JsonPropertyName("requiresClientAdminApproval")]
    public bool RequiresClientAdminApproval { get; set; }

    /// <summary>
    /// Priority for rule matching when multiple rules could apply (lower number = higher priority)
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Priority must be 1 or greater")]
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Enables time-based filtering for this flow rule (disabled by default)
    /// </summary>
    [JsonPropertyName("enableTimeBasedRules")]
    public bool EnableTimeBasedRules { get; set; } = false;

    /// <summary>
    /// Start time for active period (e.g., 09:00). Only applies if EnableTimeBasedRules is true
    /// </summary>
    [JsonPropertyName("activeStartTime")]
    public TimeSpan? ActiveStartTime { get; set; }

    /// <summary>
    /// End time for active period (e.g., 17:00). Only applies if EnableTimeBasedRules is true
    /// </summary>
    [JsonPropertyName("activeEndTime")]
    public TimeSpan? ActiveEndTime { get; set; }

    /// <summary>
    /// Days of week when this rule is active (e.g., Monday-Friday). Only applies if EnableTimeBasedRules is true
    /// </summary>
    [JsonPropertyName("activeDaysOfWeek")]
    public DayOfWeek[]? ActiveDaysOfWeek { get; set; }

    /// <summary>
    /// Validates the flow rule request
    /// </summary>
    /// <returns>Collection of validation error messages</returns>
    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (MinAmount < 0)
            errors.Add("Minimum amount must be 0 or greater");

        if (MaxAmount <= MinAmount)
            errors.Add("Maximum amount must be greater than minimum amount");

        if (EnableTimeBasedRules)
        {
            if (ActiveStartTime.HasValue && ActiveEndTime.HasValue && ActiveEndTime <= ActiveStartTime)
                errors.Add("Active end time must be after active start time");
        }

        return errors;
    }
}
