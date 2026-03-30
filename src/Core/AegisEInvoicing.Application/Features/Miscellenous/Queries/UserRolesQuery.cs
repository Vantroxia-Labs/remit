using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.Miscellenous.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.Miscellenous.Queries;

public record UserRolesQuery() : IRequest<PaginatedList<UserRolesSummaryDto>>;
