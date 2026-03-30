using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.UserManagement.Queries.GetBusinessUsers;

public record GetBusinessUsersQuery(string businessId, int pageNumber = 1,int pageSize = 20) : IRequest<PaginatedList<UserDto>>;
