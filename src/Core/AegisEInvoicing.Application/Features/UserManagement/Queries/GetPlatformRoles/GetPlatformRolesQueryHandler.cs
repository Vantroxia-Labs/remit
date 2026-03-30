using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using AegisEInvoicing.Domain.Entities.UserManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Queries.GetPlatformRoles;

public class GetPlatformRolesQueryHandler : IRequestHandler<GetPlatformRolesQuery, PaginatedList<PlatformRoleDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPlatformRolesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<PlatformRoleDto>> Handle(GetPlatformRolesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PlatformRoles
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(pr => pr.Category == request.Category);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(pr => pr.IsActive == request.IsActive.Value);
        }

        if (request.IsSystemRole.HasValue)
        {
            query = query.Where(pr => pr.IsSystemRole == request.IsSystemRole.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(pr => 
                pr.Name.ToLower().Contains(searchTerm) ||
                pr.Description.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var roles = await query
            .OrderBy(pr => pr.Category)
            .ThenBy(pr => pr.SortOrder)
            .ThenBy(pr => pr.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(pr => new PlatformRoleDto(
                pr.Id,
                pr.Name,
                pr.Description,
                pr.Category,
                pr.SortOrder,
                pr.IsSystemRole,
                pr.IsActive,
                pr.Permissions.ToList(),
                _context.UserRoleAssignments.Count(ura => ura.PlatformRoleId == pr.Id && ura.IsActive && !ura.IsExpired()),
                pr.CreatedAt,
                pr.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<PlatformRoleDto>(roles, totalCount, request.PageNumber, request.PageSize);
    }
}