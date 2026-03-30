using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetBusinessSubscription;

public record GetBusinessSubscriptionQuery(
    Guid? BusinessId = null) : IRequest<BusinessSubscriptionDto>;