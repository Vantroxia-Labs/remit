using EInvoiceIntegrator.Domain.Entities;
using MediatR;

namespace EInvoiceIntegrator.Application.Features.System.Commands;

public record InitializeSaaSSystemCommand : IRequest<SystemSetupResult>
{
    public string OrganizationName { get; init; } = default!;
    public string AdminFirstName { get; init; } = default!;
    public string AdminLastName { get; init; } = default!;
    public string AdminEmail { get; init; } = default!;
    public string AdminPassword { get; init; } = default!;
    public bool AllowSelfOnboarding { get; init; } = true;
    public int MaxBusinessesAllowed { get; init; } = 1000;
}

public record InitializeOnPremiseSystemCommand : IRequest<SystemSetupResult>
{
    public string OrganizationName { get; init; } = default!;
    public string LicenseKey { get; init; } = default!;
    public string ContactEmail { get; init; } = default!;
    public string ContactPhone { get; init; } = default!;
    public string AdminFirstName { get; init; } = default!;
    public string AdminLastName { get; init; } = default!;
    public string AdminEmail { get; init; } = default!;
    public string AdminPassword { get; init; } = default!;
    public Guid SubscriptionKeyId { get; init; } // ID of the validated subscription key
}

public record UpdateLicenseCommand : IRequest<UpdateLicenseResult>
{
    public string LicenseKey { get; init; } = default!;
    public DateTimeOffset ExpiryDate { get; init; }
}

public record SystemSetupResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = default!;
    public Guid? AdminUserId { get; init; }
    public DeploymentMode DeploymentMode { get; init; }
}

public record UpdateLicenseResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = default!;
}