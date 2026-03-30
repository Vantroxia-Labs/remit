using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.SoftDeleteBusinessFlowRule;

public class SoftDeleteBusinessFlowRuleCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<SoftDeleteBusinessFlowRuleCommand, SoftDeleteBusinessFlowRuleResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<SoftDeleteBusinessFlowRuleResult> Handle(SoftDeleteBusinessFlowRuleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.UserId.HasValue || !_currentUser.HasRole(RoleConstants.ClientAdmin))
                return new SoftDeleteBusinessFlowRuleResult
                {
                    IsSuccess = false,
                    Message = "User authentication required or insufficient permissions"
                };

            if (!_currentUser.BusinessId.HasValue)
                return new SoftDeleteBusinessFlowRuleResult
                {
                    IsSuccess = false,
                    Message = "Business context is required"
                };

            // Find the FlowRule that belongs to the current user's business and is not already deleted
            var flowRule = await _context.FlowRules
                .Where(fr => fr.Id == request.FlowRuleId && 
                           fr.BusinessId == _currentUser.BusinessId.Value && 
                           !fr.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (flowRule == null)
                return new SoftDeleteBusinessFlowRuleResult
                {
                    IsSuccess = false,
                    Message = "FlowRule not found or already deleted"
                };

            // Perform soft delete
            flowRule.DeletedBy = _currentUser.UserId.Value;
            flowRule.DeletedAt = DateTimeOffset.UtcNow;
            flowRule.IsDeleted = true;

            await _context.SaveChangesAsync(cancellationToken);

            return new SoftDeleteBusinessFlowRuleResult
            {
                IsSuccess = true,
                Message = $"FlowRule '{flowRule.Name}' has been successfully deleted",
                FlowRuleId = flowRule.Id
            };
        }
        catch (Exception ex)
        {
            return new SoftDeleteBusinessFlowRuleResult
            {
                IsSuccess = false,
                Message = $"Failed to delete FlowRule: {ex.Message}"
            };
        }
    }
}
