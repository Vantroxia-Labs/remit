using AegisEInvoicing.Application.Features.PlatformSubscriptions.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.PlatformSubscriptions.Queries.GetPlatformSubscriptionById;

public record GetPlatformSubscriptionByIdQuery : IRequest<PlatformSubscriptionDto?>
{
    public Guid PlatformSubscriptionId { get; init; }
}