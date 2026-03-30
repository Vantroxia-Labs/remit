using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessOnboarding.Queries.GetBusinessById;

public record GetBusinessByIdQuery(Guid? BusinessId = null) : IRequest<BusinessDetailDto?>;