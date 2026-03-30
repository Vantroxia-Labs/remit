using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisEInvoicing.Domain.Entities.BusinessManagement;

public class FlowRule : AuditableAggregateRoot
{
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    /// <summary>
    /// Legacy single amount field - deprecated in favor of range-based amounts
    /// </summary>
    [Obsolete("Use MinAmount and MaxAmount for range-based flow rules")]
    public double Amount { get; private set; }

    /// <summary>
    /// Minimum invoice amount for this flow rule (inclusive)
    /// </summary>
    public decimal MinAmount { get; private set; }

    /// <summary>
    /// Maximum invoice amount for this flow rule (inclusive)
    /// </summary>
    public decimal MaxAmount { get; private set; }

    /// <summary>
    /// Indicates whether this flow rule requires ClientAdmin approval
    /// If false, invoices matching this rule are auto-approved
    /// </summary>
    public bool RequiresClientAdminApproval { get; private set; }

    /// <summary>
    /// Priority for rule matching when multiple rules could apply (lower number = higher priority)
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Enables time-based filtering for this flow rule (disabled by default)
    /// </summary>
    public bool EnableTimeBasedRules { get; private set; }

    /// <summary>
    /// Start time for active period (e.g., 09:00). Only applies if EnableTimeBasedRules is true
    /// </summary>
    public TimeSpan? ActiveStartTime { get; private set; }

    /// <summary>
    /// End time for active period (e.g., 17:00). Only applies if EnableTimeBasedRules is true
    /// </summary>
    public TimeSpan? ActiveEndTime { get; private set; }

    /// <summary>
    /// Days of week when this rule is active (e.g., Monday-Friday). Only applies if EnableTimeBasedRules is true
    /// </summary>
    public DayOfWeek[]? ActiveDaysOfWeek { get; private set; }

    public Guid BusinessId { get; private set; }
    public Business Business { get; private set; } = null!;

    private FlowRule() { } // EF Constructor

    /// <summary>
    /// Creates a FlowRule with range-based amount matching
    /// </summary>
    public static FlowRule CreateWithRange(
        string name,
        string description,
        decimal minAmount,
        decimal maxAmount,
        bool requiresClientAdminApproval,
        int priority,
        Guid businessId,
        Guid createdBy,
        bool enableTimeBasedRules = false,
        TimeSpan? activeStartTime = null,
        TimeSpan? activeEndTime = null,
        DayOfWeek[]? activeDaysOfWeek = null)
    {
        ValidateRangeInputs(name, description, minAmount, maxAmount, priority);
        ValidateTimeBasedInputs(enableTimeBasedRules, activeStartTime, activeEndTime);

        var flowRule = new FlowRule
        {
            Name = name,
            Description = description,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            RequiresClientAdminApproval = requiresClientAdminApproval,
            Priority = priority,
            EnableTimeBasedRules = enableTimeBasedRules,
            ActiveStartTime = activeStartTime,
            ActiveEndTime = activeEndTime,
            ActiveDaysOfWeek = activeDaysOfWeek,
#pragma warning disable CS0618 // Type or member is obsolete
            Amount = (double)minAmount,  // Set legacy field for backward compatibility
#pragma warning restore CS0618 // Type or member is obsolete
            BusinessId = businessId,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return flowRule;
    }


    /// <summary>
    /// Updates the FlowRule with range-based configuration
    /// </summary>
    public void UpdateWithRange(
        string name,
        string description,
        decimal minAmount,
        decimal maxAmount,
        bool requiresClientAdminApproval,
        int priority,
        Guid updatedBy,
        bool enableTimeBasedRules = false,
        TimeSpan? activeStartTime = null,
        TimeSpan? activeEndTime = null,
        DayOfWeek[]? activeDaysOfWeek = null)
    {
        ValidateRangeInputs(name, description, minAmount, maxAmount, priority, true);
        ValidateTimeBasedInputs(enableTimeBasedRules, activeStartTime, activeEndTime);

        Name = name;
        Description = description;
        MinAmount = minAmount;
        MaxAmount = maxAmount;
        RequiresClientAdminApproval = requiresClientAdminApproval;
        Priority = priority;
        EnableTimeBasedRules = enableTimeBasedRules;
        ActiveStartTime = activeStartTime;
        ActiveEndTime = activeEndTime;
        ActiveDaysOfWeek = activeDaysOfWeek;
#pragma warning disable CS0618 // Type or member is obsolete
        Amount = (double)minAmount;  // Update legacy field
#pragma warning restore CS0618 // Type or member is obsolete
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }


    private static void ValidateRangeInputs(string name, string description, decimal minAmount, decimal maxAmount, int priority, bool isUpdate = false)
    {
        if (!isUpdate)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BadRequestException("FlowRule name is required", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(description))
            throw new BadRequestException("FlowRule description is required", nameof(description));

        if (!string.IsNullOrWhiteSpace(name) && name.Length > 200)
            throw new BadRequestException("FlowRule name cannot exceed 200 characters", nameof(name));

        if (description.Length > 500)
            throw new BadRequestException("FlowRule description cannot exceed 500 characters", nameof(description));

        if (minAmount < 0)
            throw new BadRequestException("Minimum amount must be greater than or equal to 0", nameof(minAmount));

        if (maxAmount <= minAmount)
            throw new BadRequestException("Maximum amount must be greater than minimum amount", nameof(maxAmount));

        if (priority < 1)
            throw new BadRequestException("Priority must be greater than or equal to 1", nameof(priority));
    }

    private static void ValidateTimeBasedInputs(bool enableTimeBasedRules, TimeSpan? activeStartTime, TimeSpan? activeEndTime)
    {
        if (!enableTimeBasedRules)
            return;  // No validation needed if time-based rules are disabled

        if (activeStartTime.HasValue && activeEndTime.HasValue)
        {
            if (activeEndTime.Value <= activeStartTime.Value)
                throw new BadRequestException("Active end time must be after active start time");
        }
    }

    private static void ValidateInputs(string name, string description, double amount, bool isUpdate = false)
    {
        if (!isUpdate)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BadRequestException("FlowRule name is required", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(description))
            throw new BadRequestException("FlowRule description is required", nameof(description));

        if (name.Length > 200)
            throw new BadRequestException("FlowRule name cannot exceed 200 characters", nameof(name));

        if (description.Length > 500)
            throw new BadRequestException("FlowRule description cannot exceed 500 characters", nameof(description));

        if (amount <= 1000)
            throw new BadRequestException("Amount must be greater than NGN 1000");
    }
}
