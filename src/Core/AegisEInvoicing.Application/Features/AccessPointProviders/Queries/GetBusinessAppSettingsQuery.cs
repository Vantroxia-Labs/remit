using AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Queries;

public record GetBusinessAppSettingsQuery(Guid BusinessId) : IRequest<BusinessAppSettingsDto?>;
