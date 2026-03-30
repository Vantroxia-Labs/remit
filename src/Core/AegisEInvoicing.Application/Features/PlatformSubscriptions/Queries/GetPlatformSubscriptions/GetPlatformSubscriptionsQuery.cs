using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.PlatformSubscriptions.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.PlatformSubscriptions.Queries.GetPlatformSubscriptions;

public record GetPlatformSubscriptionsQuery(
    ) : IRequest<PaginatedList<PlatformSubscriptionDto>>;