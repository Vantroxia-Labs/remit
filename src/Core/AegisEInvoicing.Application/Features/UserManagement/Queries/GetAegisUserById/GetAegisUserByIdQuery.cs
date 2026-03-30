using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.UserManagement.Queries.GetAegisUserById;

/// <summary>
/// Query to retrieve a single Aegis user by ID
/// Security: Only accessible to Aegis Platform Administrators
/// </summary>
public record GetAegisUserByIdQuery(Guid UserId) : IRequest<AegisUserDto?>;
