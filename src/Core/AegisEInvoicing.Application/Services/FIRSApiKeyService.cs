using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Services;

/// <summary>
/// Implementation of FIRS API key management service.
/// Handles secure storage, retrieval, and validation of API keys for both SaaS and On-Premise deployments.
/// </summary>
public sealed class FIRSApiKeyService : IFIRSApiKeyService
{
    private readonly IApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<FIRSApiKeyService> _logger;

    public FIRSApiKeyService(
        IApplicationDbContext context,
        IEncryptionService encryptionService,
        ICurrentUserService currentUserService,
        ILogger<FIRSApiKeyService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FIRSApiConfiguration?> GetActiveConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _context.FIRSApiConfigurations
                .Where(c => c.IsActive && c.IsDefault)
                .FirstOrDefaultAsync(cancellationToken);

            if (config == null)
            {
                _logger.LogWarning("No active FIRS API configuration found");
                return null;
            }

            _logger.LogInformation("Retrieved active FIRS configuration: {ConfigName} ({Environment})", 
                config.Name, config.DeploymentType);

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active FIRS API configuration");
            throw;
        }
    }

    public async Task<FIRSApiConfiguration?> GetConfigurationAsync(Guid configurationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _context.FIRSApiConfigurations
                .FirstOrDefaultAsync(c => c.Id == configurationId, cancellationToken);

            if (config == null)
            {
                _logger.LogWarning("FIRS API configuration not found: {ConfigurationId}", configurationId);
            }

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving FIRS API configuration: {ConfigurationId}", configurationId);
            throw;
        }
    }

    public async Task<FIRSApiConfiguration?> GetConfigurationByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _context.FIRSApiConfigurations
                .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving FIRS API configuration by name: {ConfigurationName}", name);
            throw;
        }
    }

    public async Task<IEnumerable<FIRSApiConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.FIRSApiConfigurations
                .OrderByDescending(c => c.IsDefault)
                .ThenByDescending(c => c.IsActive)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all FIRS API configurations");
            throw;
        }
    }

    public async Task<FIRSApiConfiguration> CreateSaaSConfigurationAsync(
        string name,
        string description,
        string apiKey,
        string apiSecret,
        string env,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if a configuration with the same name already exists
            var existingConfig = await GetConfigurationByNameAsync(name, cancellationToken);
            if (existingConfig != null)
            {
                _logger.LogWarning("FIRS API configuration with name '{ConfigName}' already exists. Skipping creation.", name);
                return existingConfig;
            }

            var encryptedApiKey = await _encryptionService.EncryptAsync(apiKey);
            var encryptedApiSecret = await _encryptionService.EncryptAsync(apiSecret);

            var configuration = FIRSApiConfiguration.CreateForSaaS(
                name, description,
                encryptedApiKey, encryptedApiSecret, env, baseUrl);

            _context.FIRSApiConfigurations.Add(configuration);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created SaaS FIRS API configuration: {ConfigName} ({Environment})", 
                name, configuration.DeploymentType);

            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SaaS FIRS API configuration: {ConfigName}", name);
            throw;
        }
    }

    public async Task<FIRSApiConfiguration> CreateOnPremiseConfigurationAsync(
        string name,
        string description,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if a configuration with the same name already exists
            var existingConfig = await GetConfigurationByNameAsync(name, cancellationToken);
            if (existingConfig != null)
            {
                _logger.LogWarning("FIRS API configuration with name '{ConfigName}' already exists. Skipping creation.", name);
                return existingConfig;
            }

            var encryptedApiKey = await _encryptionService.EncryptAsync(apiKey);
            var encryptedApiSecret = await _encryptionService.EncryptAsync(apiSecret);

            var configuration = FIRSApiConfiguration.CreateForOnPremise(
                name, description, encryptedApiKey, encryptedApiSecret, string.Empty, string.Empty);

            _context.FIRSApiConfigurations.Add(configuration);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created On-Premise FIRS API configuration: {ConfigName} ({Environment}) - Requires KMPG approval", 
                name, configuration.DeploymentType);

            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating On-Premise FIRS API configuration: {ConfigName}", name);
            throw;
        }
    }

    public async Task<bool> SetDefaultConfigurationAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await GetConfigurationAsync(configurationId, cancellationToken);
            if (config is null) return false;

            // Remove default from all other configurations
            var currentDefaults = await _context.FIRSApiConfigurations
                .Where(c => c.IsDefault && c.Id != configurationId)
                .ToListAsync(cancellationToken);

            foreach (var defaultConfig in currentDefaults)
            {
                defaultConfig.RemoveAsDefault();
            }

            // Set new default
            config.SetAsDefault();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Set FIRS configuration as default: {ConfigName}", config.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default FIRS API configuration: {ConfigurationId}", configurationId);
            return false;
        }
    }

    public async Task<bool> ActivateConfigurationAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await GetConfigurationAsync(configurationId, cancellationToken);
            if (config is null) return false;

            config.Activate();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Activated FIRS configuration: {ConfigName}", config.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating FIRS API configuration: {ConfigurationId}", configurationId);
            return false;
        }
    }

    public async Task<bool> DeactivateConfigurationAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await GetConfigurationAsync(configurationId, cancellationToken);
            if (config == null) return false;

            config.Deactivate();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Deactivated FIRS configuration: {ConfigName}", config.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating FIRS API configuration: {ConfigurationId}", configurationId);
            return false;
        }
    }

    public async Task<bool> UpdateCredentialsAsync(
        Guid configurationId,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await GetConfigurationAsync(configurationId, cancellationToken);
            if (config == null) return false;

            var encryptedApiKey = await _encryptionService.EncryptAsync(apiKey);
            var encryptedApiSecret = await _encryptionService.EncryptAsync(apiSecret);

            config.UpdateCredentials(config.Name, config.Description, encryptedApiKey, encryptedApiSecret, string.Empty, string.Empty);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated credentials for FIRS configuration: {ConfigName}", config.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating credentials for FIRS API configuration: {ConfigurationId}", configurationId);
            return false;
        }
    }

    public async Task<bool> ValidateConfigurationAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await GetConfigurationAsync(configurationId, cancellationToken);
            return config is not null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating FIRS API configuration: {ConfigurationId}", configurationId);
            return false;
        }
    }

    public async Task<(string apiKey, string apiSecret)?> GetDecryptedCredentialsAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await GetConfigurationAsync(configurationId, cancellationToken);
            if (config is null)
            {
                return null;
            }

            var apiKey = await _encryptionService.DecryptAsync(config.EncryptedApiKey);
            var apiSecret = await _encryptionService.DecryptAsync(config.EncryptedApiSecret);

            return (apiKey, apiSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting credentials for FIRS API configuration: {ConfigurationId}", configurationId);
            return null;
        }
    }

    public async Task<bool> ValidateSubscriptionAsync(
        Guid? businessId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // For SaaS deployments, validate business subscription
            if (businessId.HasValue)
            {
                var business = await _context.Businesses
                    .FirstOrDefaultAsync(b => b.Id == businessId.Value, cancellationToken);

                if (business == null) return false;

                // Add subscription validation logic here
                // For now, return true if business exists
                return true;
            }

            // For On-Premise, check if there's a valid configuration
            var activeConfig = await GetActiveConfigurationAsync(cancellationToken);
            return activeConfig is not null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating subscription for business: {BusinessId}", businessId);
            return false;
        }
    }
}