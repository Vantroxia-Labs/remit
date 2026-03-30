using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.BusinessFlowRule;

/// <summary>
/// Command to create a new FlowRule with range-based amount matching
/// </summary>
public record CreateBusinessFlowRuleCommand(
    string Name,
    string Description,
    decimal MinAmount,
    decimal MaxAmount,
    bool RequiresClientAdminApproval,
    int Priority,
    bool EnableTimeBasedRules = false,
    TimeSpan? ActiveStartTime = null,
    TimeSpan? ActiveEndTime = null,
    DayOfWeek[]? ActiveDaysOfWeek = null) : IRequest<CreateBusinessFlowRuleResult>;
