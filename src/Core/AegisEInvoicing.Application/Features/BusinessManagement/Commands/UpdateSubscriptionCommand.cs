using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands;

public record UpdateSubscriptionCommand : IRequest<UpdateSubscriptionResult>
{
    public Guid BusinessId { get; init; }
    public Guid PlatformSubscriptionId { get; init; }
}

public record UpdateSubscriptionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = default!;
}