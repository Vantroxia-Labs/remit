namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for validating FlowRule configurations to ensure business rules are maintained
/// </summary>
public interface IFlowRuleValidationService
{
    /// <summary>
    /// Validates that a new or updated FlowRule doesn't create overlapping amount ranges with existing rules
    /// </summary>
    /// <param name="businessId">The business ID</param>
    /// <param name="minAmount">Minimum amount of the range</param>
    /// <param name="maxAmount">Maximum amount of the range</param>
    /// <param name="excludeFlowRuleId">Optional FlowRule ID to exclude from validation (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with details</returns>
    Task<FlowRuleValidationResult> ValidateRangeOverlapAsync(
        Guid businessId,
        decimal minAmount,
        decimal maxAmount,
        Guid? excludeFlowRuleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a FlowRule name is unique within a business
    /// </summary>
    /// <param name="businessId">The business ID</param>
    /// <param name="name">FlowRule name to validate</param>
    /// <param name="excludeFlowRuleId">Optional FlowRule ID to exclude from validation (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<FlowRuleValidationResult> ValidateNameUniquenessAsync(
        Guid businessId,
        string name,
        Guid? excludeFlowRuleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a business has complete FlowRule coverage (no gaps in amount ranges)
    /// </summary>
    /// <param name="businessId">The business ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with coverage details</returns>
    Task<FlowRuleValidationResult> ValidateCompleteCoverageAsync(
        Guid businessId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs comprehensive validation for a new FlowRule
    /// </summary>
    /// <param name="businessId">The business ID</param>
    /// <param name="name">FlowRule name</param>
    /// <param name="minAmount">Minimum amount</param>
    /// <param name="maxAmount">Maximum amount</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive validation result</returns>
    Task<FlowRuleValidationResult> ValidateNewFlowRuleAsync(
        Guid businessId,
        string name,
        decimal minAmount,
        decimal maxAmount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs comprehensive validation for updating an existing FlowRule
    /// </summary>
    /// <param name="flowRuleId">The FlowRule ID being updated</param>
    /// <param name="businessId">The business ID</param>
    /// <param name="name">Updated FlowRule name</param>
    /// <param name="minAmount">Updated minimum amount</param>
    /// <param name="maxAmount">Updated maximum amount</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive validation result</returns>
    Task<FlowRuleValidationResult> ValidateUpdateFlowRuleAsync(
        Guid flowRuleId,
        Guid businessId,
        string name,
        decimal minAmount,
        decimal maxAmount,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of FlowRule validation
/// </summary>
public class FlowRuleValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object> ValidationDetails { get; set; } = new();

    public static FlowRuleValidationResult Success(string message = "Validation successful")
    {
        return new FlowRuleValidationResult
        {
            IsValid = true,
            Message = message
        };
    }

    public static FlowRuleValidationResult Failure(string message, params string[] errors)
    {
        return new FlowRuleValidationResult
        {
            IsValid = false,
            Message = message,
            Errors = errors.ToList()
        };
    }

    public static FlowRuleValidationResult Failure(string message, List<string> errors)
    {
        return new FlowRuleValidationResult
        {
            IsValid = false,
            Message = message,
            Errors = errors
        };
    }
}
