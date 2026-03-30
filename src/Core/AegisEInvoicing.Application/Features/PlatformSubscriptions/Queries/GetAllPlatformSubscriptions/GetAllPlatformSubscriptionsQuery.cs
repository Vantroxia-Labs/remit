using AegisEInvoicing.Application.Features.PlatformSubscriptions.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.PlatformSubscriptions.Queries.GetAllPlatformSubscriptions;

public record GetAllPlatformSubscriptionsQuery() : IRequest<List<PlatformSubscriptionDto>>;
