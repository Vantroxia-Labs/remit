namespace AegisEInvoicing.Portal.API.Models.SystemSetup;

public record InitializeOnPremiseRequest
{
    public string SubscriptionKey { get; init; } = default!;
    public string? ContactPhone { get; init; } // Optional override for admin contact phone
    public string AdminFirstName { get; init; } = default!;
    public string AdminLastName { get; init; } = default!;
    public string AdminEmail { get; init; } = default!;
    public string AdminPassword { get; init; } = default!;
}
