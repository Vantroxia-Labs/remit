using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.Authentication.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<UserDto?>;