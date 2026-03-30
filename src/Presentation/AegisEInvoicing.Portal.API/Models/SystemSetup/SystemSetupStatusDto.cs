using AegisEInvoicing.Domain.Entities;

namespace AegisEInvoicing.Portal.API.Models.SystemSetup;

public record SystemSetupStatusDto
{
    public bool IsSetupRequired { get; init; }
    public bool IsSetupCompleted { get; init; }
    public DeploymentMode? DeploymentMode { get; init; }
    public string? OrganizationName { get; init; }
    public DateTimeOffset? SetupCompletedAt { get; init; }
    public bool? IsLicenseValid { get; init; } // For On-Premise
    public DateTimeOffset? LicenseExpiryDate { get; init; } // For On-Premise
}
