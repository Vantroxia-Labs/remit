using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetFlowRuleDetails;

public class GetFlowRuleDetailsQueryHandler : IRequestHandler<GetFlowRuleDetailsQuery, IEnumerable<FlowRuleDetailsResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFlowRuleDetailsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<FlowRuleDetailsResponseDto>> Handle(GetFlowRuleDetailsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.FlowRules.AsQueryable();

        // Apply security filters - Business users can only see their own business flow rules
        if (_currentUserService.BusinessId.HasValue)
        {
            query = query.Where(fr => fr.BusinessId == _currentUserService.BusinessId.Value && !fr.IsDeleted);
        }
        else
        {
            // If no BusinessId in context, return empty result
            return Enumerable.Empty<FlowRuleDetailsResponseDto>();
        }

        // If a specific FlowRuleId is requested, filter by it
        if (request.FlowRuleId.HasValue)
        {
            query = query.Where(fr => fr.Id == request.FlowRuleId.Value);
        }

        var flowRules = await query
            .ToListAsync(cancellationToken);

        // Map to simplified DTOs with only essential information
        var flowRuleDtos = flowRules.Select(fr =>
        {
            return new FlowRuleDetailsResponseDto
            {
                Id = fr.Id,
                Name = fr.Name,
                Description = fr.Description,
                MinAmount = fr.MinAmount,
                MaxAmount = fr.MaxAmount,
                RequiresClientAdminApproval = fr.RequiresClientAdminApproval,
                Priority = fr.Priority,
                EnableTimeBasedRules = fr.EnableTimeBasedRules,
                ActiveStartTime = fr.ActiveStartTime,
                ActiveEndTime = fr.ActiveEndTime,
                ActiveDaysOfWeek = fr.ActiveDaysOfWeek
            };
        }).ToList();

        return flowRuleDtos;
    }
}
