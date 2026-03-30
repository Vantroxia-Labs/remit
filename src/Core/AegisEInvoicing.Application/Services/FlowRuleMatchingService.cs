using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Services;

/// <summary>
/// Service for matching invoices to applicable FlowRules based on amount, priority, and time-based rules
/// </summary>
public class FlowRuleMatchingService : IFlowRuleMatchingService
{
    private readonly IApplicationDbContext _context;

    public FlowRuleMatchingService(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<FlowRule?> FindMatchingFlowRuleAsync(
        Guid businessId,
        decimal invoiceAmount,
        DateTimeOffset? invoiceDate = null,
        CancellationToken cancellationToken = default)
    {
        // Get all matching FlowRules ordered by priority
        var matchingRules = await FindAllMatchingFlowRulesAsync(
            businessId,
            invoiceAmount,
            invoiceDate,
            cancellationToken);

        // Return the highest priority rule (lowest priority number)
        return matchingRules.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FlowRule>> FindAllMatchingFlowRulesAsync(
        Guid businessId,
        decimal invoiceAmount,
        DateTimeOffset? invoiceDate = null,
        CancellationToken cancellationToken = default)
    {
        // Get all active FlowRules that match the amount range
        var matchingRules = await _context.FlowRules
            .Where(fr => fr.BusinessId == businessId &&
                         !fr.IsDeleted &&
                         fr.MinAmount <= invoiceAmount &&
                         fr.MaxAmount >= invoiceAmount)
            .OrderBy(fr => fr.Priority)  // Lower priority number = higher priority
            .ToListAsync(cancellationToken);

        // Apply time-based filtering if invoice date is provided
        if (invoiceDate.HasValue)
        {
            matchingRules = matchingRules
                .Where(fr => IsFlowRuleActiveAtTime(fr, invoiceDate.Value))
                .ToList();
        }

        return matchingRules;
    }

    /// <inheritdoc/>
    public bool IsFlowRuleActiveAtTime(FlowRule flowRule, DateTimeOffset checkDateTime)
    {
        // If time-based rules are disabled, the rule is always active
        if (!flowRule.EnableTimeBasedRules)
        {
            return true;
        }

        // Check day of week if configured
        if (flowRule.ActiveDaysOfWeek != null && flowRule.ActiveDaysOfWeek.Any())
        {
            var dayOfWeek = checkDateTime.DayOfWeek;
            if (!flowRule.ActiveDaysOfWeek.Contains(dayOfWeek))
            {
                return false;  // Not active on this day
            }
        }

        // Check time range if configured
        if (flowRule.ActiveStartTime.HasValue && flowRule.ActiveEndTime.HasValue)
        {
            var checkTime = checkDateTime.TimeOfDay;
            var startTime = flowRule.ActiveStartTime.Value;
            var endTime = flowRule.ActiveEndTime.Value;

            // Handle time ranges that don't cross midnight
            if (startTime <= endTime)
            {
                if (checkTime < startTime || checkTime > endTime)
                {
                    return false;  // Outside active time range
                }
            }
            else  // Handle time ranges that cross midnight (e.g., 22:00 - 06:00)
            {
                if (checkTime < startTime && checkTime > endTime)
                {
                    return false;  // Outside active time range
                }
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task<FlowRule?> FindAutoApprovalFlowRuleAsync(
        Guid businessId,
        decimal invoiceAmount,
        CancellationToken cancellationToken = default)
    {
        // Find all matching rules
        var matchingRules = await FindAllMatchingFlowRulesAsync(
            businessId,
            invoiceAmount,
            DateTimeOffset.UtcNow,
            cancellationToken);

        // Return the first rule that doesn't require ClientAdmin approval
        return matchingRules.FirstOrDefault(fr => !fr.RequiresClientAdminApproval);
    }

    /// <inheritdoc/>
    public async Task<bool> RequiresClientAdminApprovalAsync(
        Guid businessId,
        decimal invoiceAmount,
        CancellationToken cancellationToken = default)
    {
        // Find the matching FlowRule
        var matchingRule = await FindMatchingFlowRuleAsync(
            businessId,
            invoiceAmount,
            DateTimeOffset.UtcNow,
            cancellationToken);

        // If no rule found, throw exception (business must always have matching FlowRule)
        if (matchingRule == null)
        {
            throw new InvalidOperationException(
                $"No FlowRule found for business {businessId} with invoice amount {invoiceAmount:N2}. " +
                "Please ensure complete FlowRule coverage is configured.");
        }

        return matchingRule.RequiresClientAdminApproval;
    }
}
