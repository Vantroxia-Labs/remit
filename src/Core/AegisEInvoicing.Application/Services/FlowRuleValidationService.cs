using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Services;

/// <summary>
/// Service for validating FlowRule configurations to ensure business rules are maintained
/// </summary>
public class FlowRuleValidationService : IFlowRuleValidationService
{
    private readonly IApplicationDbContext _context;

    public FlowRuleValidationService(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<FlowRuleValidationResult> ValidateRangeOverlapAsync(
        Guid businessId,
        decimal minAmount,
        decimal maxAmount,
        Guid? excludeFlowRuleId = null,
        CancellationToken cancellationToken = default)
    {
        // Get all active FlowRules for this business (excluding the one being updated if applicable)
        var existingRules = await _context.FlowRules
            .Where(fr => fr.BusinessId == businessId &&
                         !fr.IsDeleted &&
                         (!excludeFlowRuleId.HasValue || fr.Id != excludeFlowRuleId.Value))
            .Select(fr => new { fr.Id, fr.Name, fr.MinAmount, fr.MaxAmount })
            .ToListAsync(cancellationToken);

        var errors = new List<string>();

        foreach (var rule in existingRules)
        {
            // Check for overlapping ranges
            // Overlap occurs if: (minAmount <= rule.MaxAmount) AND (maxAmount >= rule.MinAmount)
            bool overlaps = minAmount <= rule.MaxAmount && maxAmount >= rule.MinAmount;

            if (overlaps)
            {
                errors.Add($"Range {minAmount:N2}-{maxAmount:N2} overlaps with existing FlowRule '{rule.Name}' ({rule.MinAmount:N2}-{rule.MaxAmount:N2})");
            }
        }

        if (errors.Any())
        {
            return FlowRuleValidationResult.Failure(
                "FlowRule range overlaps with existing rules. Each amount range must be unique.",
                errors);
        }

        return FlowRuleValidationResult.Success("No range overlaps detected");
    }

    /// <inheritdoc/>
    public async Task<FlowRuleValidationResult> ValidateNameUniquenessAsync(
        Guid businessId,
        string name,
        Guid? excludeFlowRuleId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.FlowRules
            .AnyAsync(fr => fr.BusinessId == businessId &&
                           fr.Name == name &&
                           !fr.IsDeleted &&
                           (!excludeFlowRuleId.HasValue || fr.Id != excludeFlowRuleId.Value),
                     cancellationToken);

        if (exists)
        {
            return FlowRuleValidationResult.Failure(
                $"A FlowRule with the name '{name}' already exists for this business. Please choose a different name.");
        }

        return FlowRuleValidationResult.Success("FlowRule name is unique");
    }

    /// <inheritdoc/>
    public async Task<FlowRuleValidationResult> ValidateCompleteCoverageAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        // Get all active FlowRules for this business ordered by MinAmount
        var rules = await _context.FlowRules
            .Where(fr => fr.BusinessId == businessId && !fr.IsDeleted)
            .OrderBy(fr => fr.MinAmount)
            .Select(fr => new { fr.MinAmount, fr.MaxAmount })
            .ToListAsync(cancellationToken);

        if (!rules.Any())
        {
            return FlowRuleValidationResult.Failure(
                "No active FlowRules found for this business. At least one FlowRule is required.");
        }

        // Check if the first rule starts at 0
        if (rules.First().MinAmount != 0)
        {
            return FlowRuleValidationResult.Failure(
                $"Coverage gap detected: No FlowRule covers amounts from 0 to {rules.First().MinAmount:N2}. " +
                "The first FlowRule must start at 0 to ensure complete coverage.",
                new List<string> { $"Gap: 0 - {rules.First().MinAmount:N2}" });
        }

        // Check for gaps between rules
        var gaps = new List<string>();
        for (int i = 0; i < rules.Count - 1; i++)
        {
            var currentRule = rules[i];
            var nextRule = rules[i + 1];

            // If there's a gap between current rule's max and next rule's min
            if (currentRule.MaxAmount < nextRule.MinAmount)
            {
                gaps.Add($"Gap: {currentRule.MaxAmount:N2} - {nextRule.MinAmount:N2}");
            }
        }

        if (gaps.Any())
        {
            return FlowRuleValidationResult.Failure(
                "Coverage gaps detected between FlowRules. All amount ranges must be continuous with no gaps.",
                gaps);
        }

        return FlowRuleValidationResult.Success("Complete FlowRule coverage validated");
    }

    /// <inheritdoc/>
    public async Task<FlowRuleValidationResult> ValidateNewFlowRuleAsync(
        Guid businessId,
        string name,
        decimal minAmount,
        decimal maxAmount,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Validate name uniqueness
        var nameValidation = await ValidateNameUniquenessAsync(businessId, name, null, cancellationToken);
        if (!nameValidation.IsValid)
        {
            errors.AddRange(nameValidation.Errors);
        }

        // Validate range overlap
        var rangeValidation = await ValidateRangeOverlapAsync(businessId, minAmount, maxAmount, null, cancellationToken);
        if (!rangeValidation.IsValid)
        {
            errors.AddRange(rangeValidation.Errors);
        }

        if (errors.Any())
        {
            return FlowRuleValidationResult.Failure(
                "FlowRule validation failed. Please correct the errors and try again.",
                errors);
        }

        return FlowRuleValidationResult.Success("FlowRule validation passed");
    }

    /// <inheritdoc/>
    public async Task<FlowRuleValidationResult> ValidateUpdateFlowRuleAsync(
        Guid flowRuleId,
        Guid businessId,
        string name,
        decimal minAmount,
        decimal maxAmount,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Validate the FlowRule exists
        var exists = await _context.FlowRules
            .AnyAsync(fr => fr.Id == flowRuleId && !fr.IsDeleted, cancellationToken);

        if (!exists)
        {
            return FlowRuleValidationResult.Failure($"FlowRule with ID {flowRuleId} not found or has been deleted.");
        }

        // Validate name uniqueness (excluding current FlowRule)
        var nameValidation = await ValidateNameUniquenessAsync(businessId, name, flowRuleId, cancellationToken);
        if (!nameValidation.IsValid)
        {
            errors.AddRange(nameValidation.Errors);
        }

        // Validate range overlap (excluding current FlowRule)
        var rangeValidation = await ValidateRangeOverlapAsync(businessId, minAmount, maxAmount, flowRuleId, cancellationToken);
        if (!rangeValidation.IsValid)
        {
            errors.AddRange(rangeValidation.Errors);
        }

        if (errors.Any())
        {
            return FlowRuleValidationResult.Failure(
                "FlowRule update validation failed. Please correct the errors and try again.",
                errors);
        }

        return FlowRuleValidationResult.Success("FlowRule update validation passed");
    }
}
