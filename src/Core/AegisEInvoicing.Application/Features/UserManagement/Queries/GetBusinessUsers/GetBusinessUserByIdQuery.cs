using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.UserManagement.Queries.GetBusinessUsers;

public record GetBusinessUserByIdQuery(string businessId, string userId) : IRequest<UserDto?>;