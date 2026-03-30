using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Security;
using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetAllFlowRules;

public class GetAllFlowRulesQueryHandler : IRequestHandler<GetAllFlowRulesQuery, IEnumerable<FlowRuleDetailsResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAllFlowRulesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<FlowRuleDetailsResponseDto>> Handle(GetAllFlowRulesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.FlowRules.AsQueryable();

        // Apply security filters based on user role
        if (_currentUserService.HasRole(RoleConstants.AegisAdmin))
        {
            // System admin can see all flow rules from all businesses
        }
        else if (_currentUserService.BusinessId.HasValue)
        {
            // Business users can only see their own business flow rules
            query = query.Where(fr => fr.BusinessId == _currentUserService.BusinessId.Value);
        }
        else
        {
            // If no BusinessId in context and not system admin, return empty result
            return Enumerable.Empty<FlowRuleDetailsResponseDto>();
        }

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            // Sanitize search term to prevent SQL injection (VAPT finding)
            var searchTerm = InputSanitizationService.SanitizeSearchTerm(request.SearchTerm);
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(fr =>
                    fr.Name.ToLower().Contains(searchTerm) ||
                    fr.Description.ToLower().Contains(searchTerm));
            }
        }

        // Apply sorting
#pragma warning disable CS0618 // Type or member is obsolete
        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDescending ? query.OrderByDescending(fr => fr.Name) : query.OrderBy(fr => fr.Name),
            "description" => request.SortDescending ? query.OrderByDescending(fr => fr.Description) : query.OrderBy(fr => fr.Description),
            "amount" => request.SortDescending ? query.OrderByDescending(fr => fr.Amount) : query.OrderBy(fr => fr.Amount),
            "createdat" => request.SortDescending ? query.OrderByDescending(fr => fr.CreatedAt) : query.OrderBy(fr => fr.CreatedAt),
            "updatedat" => request.SortDescending ? query.OrderByDescending(fr => fr.UpdatedAt) : query.OrderBy(fr => fr.UpdatedAt),
            _ => query.OrderByDescending(fr => fr.CreatedAt) // Default sort by creation date descending
        };
#pragma warning restore CS0618 // Type or member is obsolete

        // Apply pagination
        var skip = (request.PageNumber - 1) * request.PageSize;
        query = query.Skip(skip).Take(request.PageSize);

        var flowRules = await query.ToListAsync(cancellationToken);

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
