using AegisEInvoicing.Domain.Entities;

namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for managing FIRS API keys across different deployment scenarios.
/// Handles both SaaS (Aegis-managed) and On-Premise (customer-managed but Aegis-controlled) configurations.
/// </summary>
public interface IFIRSApiKeyService
{
    /// <summary>
    /// Gets the active FIRS API configuration for the current deployment context
    /// </summary>
    Task<FIRSApiConfiguration?> GetActiveConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets FIRS API configuration by ID
    /// </summary>
    Task<FIRSApiConfiguration?> GetConfigurationAsync(Guid configurationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets FIRS API configuration by name
    /// </summary>
    Task<FIRSApiConfiguration?> GetConfigurationByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all FIRS API configurations (for Aegis admin use)
    /// </summary>
    Task<IEnumerable<FIRSApiConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new FIRS API configuration for SaaS deployment
    /// </summary>
    Task<FIRSApiConfiguration> CreateSaaSConfigurationAsync(
        string name,
        string description,
        string apiKey,
        string apiSecret,
        string env,
        string baseUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new FIRS API configuration for On-Premise deployment
    /// </summary>
    Task<FIRSApiConfiguration> CreateOnPremiseConfigurationAsync(
        string name,
        string description,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a configuration as the default for the current deployment
    /// </summary>
    Task<bool> SetDefaultConfigurationAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a configuration
    /// </summary>
    Task<bool> ActivateConfigurationAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a configuration
    /// </summary>
    Task<bool> DeactivateConfigurationAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates API credentials for a configuration
    /// </summary>
    Task<bool> UpdateCredentialsAsync(
        Guid configurationId,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a configuration can be used (active, not expired, within limits)
    /// </summary>
    Task<bool> ValidateConfigurationAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets decrypted API credentials for a configuration
    /// </summary>
    Task<(string apiKey, string apiSecret)?> GetDecryptedCredentialsAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default);
      
    /// <summary>
    /// Validates subscription and checks if FIRS access is allowed
    /// </summary>
    Task<bool> ValidateSubscriptionAsync(
        Guid? businessId = null,
        CancellationToken cancellationToken = default);
}