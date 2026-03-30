namespace AegisEInvoicing.Portal.API.Models.SystemSetup;

public record InitializeSaaSRequest
{
    public string OrganizationName { get; init; } = default!;
    public string AdminFirstName { get; init; } = default!;
    public string AdminLastName { get; init; } = default!;
    public string AdminEmail { get; init; } = default!;
    public string AdminPassword { get; init; } = default!;
    public bool AllowSelfOnboarding { get; init; } = true;
    public int MaxBusinessesAllowed { get; init; } = 1000;
}
