using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.Queries.GetAllBusinessFIRSConfigurations;

public record GetAllBusinessFIRSConfigurationsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? BusinessName = null,
    string? ConfigurationName = null) : IRequest<PaginatedList<BusinessFIRSApiConfigurationDto>>;