using AegisEInvoicing.Application.Features.Miscellenous.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.Miscellenous.Queries;

public record GetRegionsQuery() : IRequest<List<RegionDto>>;
