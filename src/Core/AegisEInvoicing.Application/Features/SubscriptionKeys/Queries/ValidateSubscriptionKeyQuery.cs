using MediatR;

namespace AegisEInvoicing.Application.Features.SubscriptionKeys.Queries;

public record ValidateSubscriptionKeyQuery : IRequest<ValidateSubscriptionKeyResult>
{
    public string Key { get; init; } = default!;
}

public record ValidateSubscriptionKeyResult
{
    public bool IsValid { get; init; }
    public Guid? SubscriptionKeyId { get; init; }
    public string? BusinessName { get; init; }
    public string? ContactEmail { get; init; }
    public DateTimeOffset? ExpiryDate { get; init; }
    public int? MaxUsers { get; init; }
    public int? MaxBusinesses { get; init; }
    public string? Features { get; init; }
    public string? ValidationError { get; init; }
}