using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.BusinessFlowRule;

public class CreateBusinessFlowRuleCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IFlowRuleValidationService validationService) : IRequestHandler<CreateBusinessFlowRuleCommand, CreateBusinessFlowRuleResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IFlowRuleValidationService _validationService = validationService;

    public async Task<CreateBusinessFlowRuleResult> Handle(CreateBusinessFlowRuleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.UserId.HasValue && !_currentUser.HasRole(RoleConstants.ClientAdmin))
                return new CreateBusinessFlowRuleResult(false, "User authentication required");

            if (!_currentUser.BusinessId.HasValue)
                return new CreateBusinessFlowRuleResult(false, "User authentication required");

            var business = await _context.Businesses.SingleOrDefaultAsync(r => r.Id == _currentUser.BusinessId.Value, cancellationToken);
            if (business is null)
                return new CreateBusinessFlowRuleResult(false, "We could not find a Business for this user");

            // Upsert: if a rule already exists for this business, update it instead of creating a new one
            var existingFlowRule = await _context.FlowRules
                .FirstOrDefaultAsync(r => r.BusinessId == _currentUser.BusinessId.Value && !r.IsDeleted, cancellationToken);

            if (existingFlowRule is not null)
            {
                var updateValidation = await _validationService.ValidateUpdateFlowRuleAsync(
                    existingFlowRule.Id,
                    _currentUser.BusinessId.Value,
                    request.Name,
                    request.MinAmount,
                    request.MaxAmount,
                    cancellationToken);

                if (!updateValidation.IsValid)
                    return new CreateBusinessFlowRuleResult(false, updateValidation.Message, null, updateValidation.Errors);

                existingFlowRule.UpdateWithRange(
                    request.Name,
                    request.Description,
                    request.MinAmount,
                    request.MaxAmount,
                    request.RequiresClientAdminApproval,
                    request.Priority,
                    _currentUser.UserId!.Value,
                    request.EnableTimeBasedRules,
                    request.ActiveStartTime,
                    request.ActiveEndTime,
                    request.ActiveDaysOfWeek);

                await _context.SaveChangesAsync(cancellationToken);

                return new CreateBusinessFlowRuleResult(
                    true,
                    $"Flow Rule '{existingFlowRule.Name}' successfully updated for Business '{business.Name}' with range {request.MinAmount:N2} - {request.MaxAmount:N2}",
                    existingFlowRule.Id);
            }

            // Perform comprehensive validation for the new FlowRule
            var validationResult = await _validationService.ValidateNewFlowRuleAsync(
                _currentUser.BusinessId.Value,
                request.Name,
                request.MinAmount,
                request.MaxAmount,
                cancellationToken);

            if (!validationResult.IsValid)
            {
                return new CreateBusinessFlowRuleResult(
                    false,
                    validationResult.Message,
                    null,
                    validationResult.Errors);
            }

            // Create the FlowRule using the new range-based factory method
            var flowRule = FlowRule.CreateWithRange(
                request.Name,
                request.Description,
                request.MinAmount,
                request.MaxAmount,
                request.RequiresClientAdminApproval,
                request.Priority,
                _currentUser.BusinessId.Value,
                _currentUser.UserId!.Value,
                request.EnableTimeBasedRules,
                request.ActiveStartTime,
                request.ActiveEndTime,
                request.ActiveDaysOfWeek);

            await _context.FlowRules.AddAsync(flowRule, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            business.AssignFlowRule(flowRule.Id, _currentUser.UserId!.Value);
            await _context.SaveChangesAsync(cancellationToken);

            return new CreateBusinessFlowRuleResult(
               true,
               $"Flow Rule '{flowRule.Name}' successfully created for Business '{business.Name}' with range {request.MinAmount:N2} - {request.MaxAmount:N2}",
               flowRule.Id);

        }
        catch (Exception ex)
        {
            return new CreateBusinessFlowRuleResult(
                false,
                $"Failed to create Flow Rule: {ex.Message}");
        }
    }
}
