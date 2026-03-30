using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Queries;

public record GetAccessPointProvidersQuery(int PageNumber = 1,int PageSize = 20, string? SearchTerm = null) : IRequest<PaginatedList<AccessPointProvidersDto>>;
