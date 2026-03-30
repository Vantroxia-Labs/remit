using MediatR;

namespace AegisEInvoicing.Application.Features.SubscriptionKeys.Commands;

public record MarkSubscriptionKeyUsedCommand : IRequest<MarkSubscriptionKeyUsedResult>
{
    public Guid SubscriptionKeyId { get; init; }
    public string? Notes { get; init; }
}

public record MarkSubscriptionKeyUsedResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = default!;
}