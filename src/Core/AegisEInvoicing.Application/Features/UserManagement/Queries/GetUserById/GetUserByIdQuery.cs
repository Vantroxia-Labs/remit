using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.UserManagement.Queries.GetUserById;

public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto?>;