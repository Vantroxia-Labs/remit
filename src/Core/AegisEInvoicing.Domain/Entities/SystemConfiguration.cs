using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Entities;

/// <summary>
/// System-wide configuration settings set during initial setup
/// </summary>
public class SystemConfiguration : AuditableAggregateRoot
{
    public string OrganizationName { get; private set; } = default!;
    public DeploymentMode DeploymentMode { get; private set; }
    public bool IsSetupCompleted { get; private set; }
    public DateTimeOffset? SetupCompletedAt { get; private set; }
    public Guid SetupCompletedBy { get; private set; }
    
    // On-Premise specific settings
    public string? LicenseKey { get; private set; }
    public DateTimeOffset? LicenseExpiryDate { get; private set; }
    public string? OrganizationContactEmail { get; private set; }
    public string? OrganizationContactPhone { get; private set; }
    
    // SaaS specific settings  
    public bool AllowSelfOnboarding { get; private set; }
    public int MaxBusinessesAllowed { get; private set; }

    // Parameterless constructor for Entity Framework
    private SystemConfiguration()
    {
        IsSetupCompleted = false;
        AllowSelfOnboarding = true;
        MaxBusinessesAllowed = 1000; // Default for SaaS
    }

    public static SystemConfiguration CreateForSaaS(
        string organizationName,
        Guid setupBy,
        bool allowSelfOnboarding = true,
        int maxBusinesses = 1000)
    {
        var config = new SystemConfiguration
        {
            Id = Guid.CreateVersion7(),
            OrganizationName = organizationName,
            DeploymentMode = DeploymentMode.Cloud,
            AllowSelfOnboarding = allowSelfOnboarding,
            MaxBusinessesAllowed = maxBusinesses,
            IsSetupCompleted = true,
            SetupCompletedAt = DateTimeOffset.UtcNow,
            SetupCompletedBy = setupBy,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = setupBy
        };

        return config;
    }

    public static SystemConfiguration CreateForOnPremise(
        string organizationName,
        string licenseKey,
        DateTimeOffset licenseExpiryDate,
        string contactEmail,
        string contactPhone,
        Guid setupBy)
    {
        var config = new SystemConfiguration
        {
            Id = Guid.CreateVersion7(),
            OrganizationName = organizationName,
            DeploymentMode = DeploymentMode.OnPremise,
            LicenseKey = licenseKey,
            LicenseExpiryDate = licenseExpiryDate,
            OrganizationContactEmail = contactEmail,
            OrganizationContactPhone = contactPhone,
            AllowSelfOnboarding = false, // On-premise typically manages their own onboarding
            MaxBusinessesAllowed = 1, // On-premise is typically single organization
            IsSetupCompleted = true,
            SetupCompletedAt = DateTimeOffset.UtcNow,
            SetupCompletedBy = setupBy,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = setupBy
        };

        return config;
    }

    public void UpdateLicense(string licenseKey, DateTimeOffset expiryDate, Guid updatedBy)
    {
        if (DeploymentMode != DeploymentMode.OnPremise)
            throw new InvalidOperationException("License can only be updated for On-Premise deployments");

        LicenseKey = licenseKey;
        LicenseExpiryDate = expiryDate;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public bool IsLicenseValid()
    {
        if (DeploymentMode == DeploymentMode.Cloud)
            return true; // Cloud doesn't require license validation

        return !string.IsNullOrEmpty(LicenseKey) && 
               LicenseExpiryDate.HasValue && 
               LicenseExpiryDate.Value > DateTimeOffset.UtcNow;
    }

    public bool CanKMPGManage(BusinessFunction function)
    {
        return DeploymentMode switch
        {
            DeploymentMode.Cloud => true, // KMPG manages everything in Cloud
            DeploymentMode.OnPremise => function == BusinessFunction.SubscriptionManagement,
            _ => false
        };
    }
}

/// <summary>
/// System deployment modes - where the application runs
/// Cloud (SaaS) is hosted on Aegis infrastructure
/// </summary>
public enum DeploymentMode
{
    Cloud = 0,      // Hosted on Aegis cloud infrastructure (same as SaaS)
    OnPremise = 1   // Hosted on customer's on-premise infrastructure
}

/// <summary>
/// Business functions that can be managed
/// </summary>
public enum BusinessFunction
{
    BusinessOnboarding,
    UserManagement, 
    BusinessManagement,
    SubscriptionManagement,
    FIRSIntegration,
    Compliance,
    Analytics
}