using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.BusinessManagement;

namespace AegisEInvoicing.Domain.Entities;

/// <summary>
/// Represents FIRS API configuration for different deployment scenarios.
/// Supports both SaaS (Aegis-managed) and On-Premise (customer-managed) deployments.
/// </summary>
public class FIRSApiConfiguration : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public FIRSDeploymentType DeploymentType { get; private set; }
    
    // Encrypted API credentials
    public string EncryptedApiKey { get; private set; } = default!;
    public string EncryptedApiSecret { get; private set; } = default!;
    public string Environment { get; private set; } = default!;
    public string BaseUrl { get; private set; } = default!;

    public bool IsDefault { get; private set; }

    // Configuration metadata
    public bool IsActive { get; private set; }

    private readonly List<BusinessFIRSApiConfiguration> _businessFirsApiConfigurations = [];
    public IReadOnlyCollection<BusinessFIRSApiConfiguration> BusinessFIRSApiConfigurations => _businessFirsApiConfigurations.AsReadOnly();

    private FIRSApiConfiguration() { } // EF Constructor

    private FIRSApiConfiguration(
        string name,
        string description,
        FIRSDeploymentType deploymentType,
        string encryptedApiKey,
        string encryptedApiSecret,
        string environment,
        string baseUrl)
    {
        Name = name;
        Description = description;
        DeploymentType = deploymentType;
        EncryptedApiKey = encryptedApiKey;
        EncryptedApiSecret = encryptedApiSecret;
        IsActive = true;
        Environment = environment;
        BaseUrl = baseUrl;
    }

    public static FIRSApiConfiguration CreateForSaaS(
        string name,
        string description,
        string encryptedApiKey,
        string encryptedApiSecret,
        string env,
        string baseUrl)
    {
        return new FIRSApiConfiguration(
            name, description, FIRSDeploymentType.SaaS, 
            encryptedApiKey, encryptedApiSecret, env, baseUrl);
    }

    public static FIRSApiConfiguration CreateForOnPremise(
        string name,
        string description,
        string encryptedApiKey,
        string encryptedApiSecret,
        string env,
        string baseUrl)
    {
        var config = new FIRSApiConfiguration(
            name, description, FIRSDeploymentType.OnPremise,
            encryptedApiKey, encryptedApiSecret, env, baseUrl);
        
        return config;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
    }

    public void RemoveAsDefault()
    {
        IsDefault = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }    

    public void UpdateCredentials(string name,string description, string encryptedApiKey, string encryptedApiSecret, string env, string baseUrl)
    {
        Name = name;
        Description = description;
        EncryptedApiKey = encryptedApiKey;
        EncryptedApiSecret = encryptedApiSecret;
        Environment = env;
        BaseUrl = baseUrl;
    }
}

/// <summary>
/// Defines the deployment type for FIRS API configuration
/// </summary>
public enum FIRSDeploymentType
{
    /// <summary>
    /// Software as a Service - Aegis manages the API keys
    /// </summary>
    SaaS = 1,
    
    /// <summary>
    /// On-Premise deployment - Customer manages API keys but KMPG controls access
    /// </summary>
    OnPremise = 2
}