using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.RoleManagement.Queries.GetBusinessRoles;

/// <summary>
/// Returns roles available to the current business:
/// system roles (BusinessId == null) plus custom roles scoped to the caller's business.
/// </summary>
public record GetBusinessRolesQuery : IRequest<List<PlatformRoleDto>>;

public class GetBusinessRolesQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<GetBusinessRolesQuery, List<PlatformRoleDto>>
{
    public async Task<List<PlatformRoleDto>> Handle(
        GetBusinessRolesQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.BusinessId.HasValue)
            throw new AuthenticationException("Business context is required.");

        var businessId = currentUser.BusinessId.Value;

        var roles = await context.PlatformRoles
            .Where(r => !r.IsDeleted
                     && r.IsActive
                     && (r.BusinessId == null || r.BusinessId == businessId))
            .OrderBy(r => r.BusinessId == null ? 0 : 1) // system roles first
            .ThenBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .Select(r => new PlatformRoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.Category,
                r.SortOrder,
                r.IsSystemRole,
                r.IsActive,
                r.Permissions.ToList(),
                context.UserRoleAssignments.Count(a => a.PlatformRoleId == r.Id && a.IsActive && !(a.ExpiresAt.HasValue && a.ExpiresAt <= DateTimeOffset.UtcNow)),
                r.CreatedAt,
                r.UpdatedAt))
            .ToListAsync(cancellationToken);

        return roles;
    }
}
