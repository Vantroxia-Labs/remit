using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.Miscellenous.DTOs;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.Miscellenous.Queries;

public class PlatformBusinessRolesQueryHandler(IApplicationDbContext context) : IRequestHandler<PlatformBusinessRolesQuery, PaginatedList<PlatformBusinessRoleSummaryDto>>
{
    private readonly IApplicationDbContext _context = context;
    public async Task<PaginatedList<PlatformBusinessRoleSummaryDto>> Handle(PlatformBusinessRolesQuery request, CancellationToken cancellationToken)
    {
        List<PlatformBusinessRoleSummaryDto> roleSummaryDtos = new List<PlatformBusinessRoleSummaryDto>();

        var roles = await _context.PlatformRoles.ToListAsync(cancellationToken);

        if(!request.isBusiness)
        {
            roleSummaryDtos = roles.Select(r => new PlatformBusinessRoleSummaryDto()
            {
                Id = r.Id,
                Name = r.Name,
            }).ToList();
        }
        else
        {
            roleSummaryDtos = roles.Where(r => r.Name != RoleConstants.AegisAdmin).Select(r => new PlatformBusinessRoleSummaryDto()
            {
                Id = r.Id,
                Name = r.Name,
            }).ToList();
        }

        return new PaginatedList<PlatformBusinessRoleSummaryDto>(roleSummaryDtos, roles.Count, 1, 1);
    }
}
