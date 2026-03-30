using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetAllFlowRules;

/// <summary>
/// Query to get all flow rules with pagination support
/// </summary>
public record GetAllFlowRulesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? SortBy = null,
    bool SortDescending = false
) : IRequest<IEnumerable<FlowRuleDetailsResponseDto>>;
