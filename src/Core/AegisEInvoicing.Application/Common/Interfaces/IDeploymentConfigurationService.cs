using AegisEInvoicing.Domain.Entities;

namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service to provide deployment configuration information
/// </summary>
public interface IDeploymentConfigurationService
{
    /// <summary>
    /// Gets the current deployment mode (SaaS or OnPremise)
    /// </summary>
    string GetDeploymentMode();
    
    /// <summary>
    /// Checks if the current deployment is SaaS
    /// </summary>
    bool IsSaaSDeployment();
    
    /// <summary>
    /// Checks if the current deployment is On-Premise
    /// </summary>
    bool IsOnPremiseDeployment();
    
    /// <summary>
    /// Determines if KMPG has management access for a specific business function
    /// For On-Premise: Only subscription management
    /// For SaaS: Full management access
    /// </summary>
    bool CanKMPGManage(BusinessFunction function);
}