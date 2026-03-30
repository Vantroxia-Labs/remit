using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.Miscellenous.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.Miscellenous.Queries;

public record CitiesQuery(string stateName) : IRequest<PaginatedList<CitiesSummaryDto>>;
