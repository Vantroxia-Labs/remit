using AegisEInvoicing.Domain.Entities.BusinessManagement;

namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for matching invoices to applicable FlowRules based on amount, priority, and time-based rules
/// </summary>
public interface IFlowRuleMatchingService
{
    /// <summary>
    /// Finds the most applicable FlowRule for a given invoice amount within a business
    /// Uses priority-based selection when multiple rules could match
    /// </summary>
    /// <param name="businessId">The business ID</param>
    /// <param name="invoiceAmount">The invoice amount</param>
    /// <param name="invoiceDate">Optional invoice date for time-based rule filtering</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The matching FlowRule or null if no match found</returns>
    Task<FlowRule?> FindMatchingFlowRuleAsync(
        Guid businessId,
        decimal invoiceAmount,
        DateTimeOffset? invoiceDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all FlowRules that could apply to a given invoice amount
    /// Returns rules ordered by priority (lower number = higher priority)
    /// </summary>
    /// <param name="businessId">The business ID</param>
    /// <param name="invoiceAmount">The invoice amount</param>
    /// <param name="invoiceDate">Optional invoice date for time-based rule filtering</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching FlowRules ordered by priority</returns>
    Task<IEnumerable<FlowRule>> FindAllMatchingFlowRulesAsync(
        Guid businessId,
        decimal invoiceAmount,
        DateTimeOffset? invoiceDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a FlowRule is active based on time-based filtering rules
    /// </summary>
    /// <param name="flowRule">The FlowRule to check</param>
    /// <param name="checkDateTime">The date/time to check against</param>
    /// <returns>True if the rule is active at the given time</returns>
    bool IsFlowRuleActiveAtTime(FlowRule flowRule, DateTimeOffset checkDateTime);

    /// <summary>
    /// Gets the FlowRule that requires the least restrictive approval (for auto-approval scenarios)
    /// </summary>
    /// <param name="businessId">The business ID</param>
    /// <param name="invoiceAmount">The invoice amount</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FlowRule with RequiresClientAdminApproval = false, or null if all matching rules require approval</returns>
    Task<FlowRule?> FindAutoApprovalFlowRuleAsync(
        Guid businessId,
        decimal invoiceAmount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if an invoice amount requires ClientAdmin approval based on matching FlowRules
    /// </summary>
    /// <param name="businessId">The business ID</param>
    /// <param name="invoiceAmount">The invoice amount</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if ClientAdmin approval is required</returns>
    Task<bool> RequiresClientAdminApprovalAsync(
        Guid businessId,
        decimal invoiceAmount,
        CancellationToken cancellationToken = default);
}
