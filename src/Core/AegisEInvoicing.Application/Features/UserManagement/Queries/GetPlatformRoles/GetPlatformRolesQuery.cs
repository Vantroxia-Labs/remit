using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.UserManagement.Queries.GetPlatformRoles;

public record GetPlatformRolesQuery(
    string? Category = null,
    bool? IsActive = null,
    bool? IsSystemRole = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PaginatedList<PlatformRoleDto>>;