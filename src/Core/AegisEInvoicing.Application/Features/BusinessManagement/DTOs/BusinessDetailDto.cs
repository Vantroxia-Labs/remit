using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.BusinessManagement.DTOs;

public record BusinessDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Industry { get; init; } = default!;
    public string BusinessRegistrationNumber { get; init; } = default!;
    public string TIN { get; init; } = default!;
    public string ServiceId { get; init; } = default!;
    public string InvoicePrefix { get; init; } = default!;
    public AddressDto RegisteredAddress { get; init; } = default!;
    public string ContactEmail { get; init; } = default!;
    public string ContactPhone { get; init; } = default!;
    public Guid FIRSBusinessId { get; init; }
    public BusinessStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public BusinessSubscriptionDto? SubscriptionInfo { get; init; }
    public int UserCount { get; init; }
    public bool HasNrsCredentials { get; init; }
    public bool HasQrCodeConfig { get; init; }
}
