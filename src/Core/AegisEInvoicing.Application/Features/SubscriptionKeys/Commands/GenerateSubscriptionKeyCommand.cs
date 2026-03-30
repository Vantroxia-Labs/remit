using MediatR;

namespace AegisEInvoicing.Application.Features.SubscriptionKeys.Commands;

public record GenerateSubscriptionKeyCommand : IRequest<GenerateSubscriptionKeyResult>
{
    public string BusinessName { get; init; } = default!;
    public string ContactEmail { get; init; } = default!;
    public string? ContactPhone { get; init; }
    public DateTimeOffset ExpiryDate { get; init; }
    public int MaxUsers { get; init; } = 10;
    public int MaxBusinesses { get; init; } = 1;
    public string? Features { get; init; }
}

public record GenerateSubscriptionKeyResult
{
    public Guid SubscriptionKeyId { get; init; }
    public string Key { get; init; } = default!;
    public string BusinessName { get; init; } = default!;
    public DateTimeOffset ExpiryDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}