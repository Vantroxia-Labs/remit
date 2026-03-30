using AegisEInvoicing.Domain.Entities;

namespace AegisEInvoicing.Portal.API.Models.SystemSetup;

public record SystemSetupResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = default!;
    public Guid? AdminUserId { get; init; }
    public DeploymentMode DeploymentMode { get; init; }
}
