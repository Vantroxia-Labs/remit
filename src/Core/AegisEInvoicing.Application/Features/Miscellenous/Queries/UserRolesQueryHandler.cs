using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.Miscellenous.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using System.Data;

namespace AegisEInvoicing.Application.Features.Miscellenous.Queries;

public class UserRolesQueryHandler : IRequestHandler<UserRolesQuery, PaginatedList<UserRolesSummaryDto>>
{   

    public async Task<PaginatedList<UserRolesSummaryDto>> Handle(UserRolesQuery request, CancellationToken cancellationToken)
    {
        List<UserRolesSummaryDto> userRolesSummaryDtos = new List<UserRolesSummaryDto>();

        string[] AegisRoles = Enum.GetNames(typeof(AegisRole));

        List<string> roleList = new List<string>(AegisRoles);

        userRolesSummaryDtos.AddRange(roleList.Select(r => new UserRolesSummaryDto() { Name = r }));

        return new PaginatedList<UserRolesSummaryDto>(await Task.FromResult(userRolesSummaryDtos), roleList.Count, 1, 1);
    }
}
