using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using AegisEInvoicing.Domain.Entities.UserManagement;
using MediatR;

namespace AegisEInvoicing.Application.Features.UserManagement.Queries.GetUsers;

public record GetUsersQuery(
    Guid? BusinessId = null,
    Guid? BranchId = null,
    string? SearchTerm = null,
    UserStatus? Status = null,
    bool? IsEmailVerified = null,
    bool? MustChangePassword = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<UserDto>>;