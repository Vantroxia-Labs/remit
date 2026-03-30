using AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.Queries.GetBusinessFIRSConfiguration;

public record GetBusinessFIRSConfigurationQuery() 
    : IRequest<BusinessFIRSApiConfigurationDetailDto?>;