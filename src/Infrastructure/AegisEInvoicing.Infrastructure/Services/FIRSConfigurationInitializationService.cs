using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.Services;

/// <summary>
/// Background service that initializes FIRS API configurations on application startup.
/// Sets up default configurations based on deployment mode (SaaS or On-Premise).
/// </summary>
public class FIRSConfigurationInitializationService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<FIRSConfigurationInitializationService> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ILogger<FIRSConfigurationInitializationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Wait for a short delay to ensure services are fully initialized
            await Task.Delay(5000, stoppingToken);

            await InitializeFIRSConfigurationAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during FIRS configuration initialization");
        }
    }

    private async Task InitializeFIRSConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var firsApiKeyService = scope.ServiceProvider.GetRequiredService<IFIRSApiKeyService>();

            _logger.LogInformation("Starting FIRS API configuration initialization...");

            // Check if any active configuration already exists
            var existingConfig = await firsApiKeyService.GetActiveConfigurationAsync(cancellationToken);
            if (existingConfig != null)
            {
                _logger.LogInformation("Active FIRS configuration already exists: {ConfigName} ({DeploymentType})", 
                    existingConfig.Name, existingConfig.DeploymentType);
                return;
            }

            // Get deployment mode from configuration
            var deploymentMode = _configuration["DeploymentMode"]?.ToLowerInvariant();
            
            if (string.IsNullOrEmpty(deploymentMode))
            {
                _logger.LogWarning("DeploymentMode not specified in configuration. Skipping FIRS configuration initialization.");
                return;
            }

            switch (deploymentMode)
            {
                case "saas":
                    await InitializeSaaSConfigurationAsync(firsApiKeyService, cancellationToken);
                    break;

                case "onpremise":
                case "on-premise":
                    await InitializeOnPremiseConfigurationAsync(firsApiKeyService, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unknown deployment mode: {DeploymentMode}. Skipping initialization.", deploymentMode);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing FIRS API configuration");
            throw;
        }
    }

    private async Task InitializeSaaSConfigurationAsync(
        IFIRSApiKeyService firsApiKeyService,
        CancellationToken cancellationToken)
    {
        try
        {
            var configSection = _configuration.GetSection("FIRSApiConfiguration:DefaultConfiguration");
            if (!configSection.Exists())
            {
                _logger.LogWarning("SaaS FIRS configuration section not found. Skipping initialization.");
                return;
            }

            var name = configSection["Name"] ?? "Default SaaS Configuration";
            var description = configSection["Description"] ?? "Default FIRS API configuration for SaaS deployment";
            var environment = configSection["Environment"] ?? "Production";
            var baseUrl = configSection["BaseUrl"] ?? "https://api.firs.gov.ng";
            var apiKey = configSection["ApiKey"];
            var apiSecret = configSection["ApiSecret"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                _logger.LogWarning("SaaS FIRS API credentials not configured. Please set ApiKey and ApiSecret in configuration.");
                return;
            }

            // Parse limits with defaults
            var dailyLimit = int.TryParse(configSection["DailyRequestLimit"], out var daily) ? daily : 50000;
            var monthlyLimit = int.TryParse(configSection["MonthlyRequestLimit"], out var monthly) ? monthly : 1500000;

            // Check if configuration with this name already exists
            var existingConfig = await firsApiKeyService.GetConfigurationByNameAsync(name, cancellationToken);
            
            FIRSApiConfiguration configuration;
            if (existingConfig != null)
            {
                _logger.LogInformation("SaaS FIRS configuration already exists: {ConfigName}. Using existing configuration.", name);
                configuration = existingConfig;
            }
            else
            {
                _logger.LogInformation("Creating default SaaS FIRS configuration: {ConfigName}", name);
                configuration = await firsApiKeyService.CreateSaaSConfigurationAsync(
                    name,
                    description,
                    apiKey,
                    apiSecret,
                    environment, 
                    baseUrl,
                    cancellationToken);
            }

            // Ensure it's set as default and activated
            await firsApiKeyService.SetDefaultConfigurationAsync(configuration.Id, cancellationToken);
            await firsApiKeyService.ActivateConfigurationAsync(configuration.Id, cancellationToken);

            _logger.LogInformation("Successfully initialized SaaS FIRS configuration: {ConfigId}", configuration.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing SaaS FIRS configuration");
            throw;
        }
    }

    private async Task InitializeOnPremiseConfigurationAsync(
        IFIRSApiKeyService firsApiKeyService,
        CancellationToken cancellationToken)
    {
        try
        {
            var configSection = _configuration.GetSection("FIRSApiConfiguration:DefaultConfiguration");
            if (!configSection.Exists())
            {
                _logger.LogWarning("On-Premise FIRS configuration section not found. Skipping initialization.");
                return;
            }

            var name = configSection["Name"] ?? "Default On-Premise Configuration";
            var description = configSection["Description"] ?? "Default FIRS API configuration for On-Premise deployment";
            var environment = configSection["Environment"] ?? "Production";
            var baseUrl = configSection["BaseUrl"] ?? "https://api.firs.gov.ng";
            var apiKey = configSection["ApiKey"];
            var apiSecret = configSection["ApiSecret"];
            var allowedDomains = configSection["AllowedDomains"] ?? "[]";

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                _logger.LogInformation("On-Premise FIRS API credentials not configured. Configuration will need to be set up manually.");
                return;
            }

            // Parse limits with defaults
            var dailyLimit = int.TryParse(configSection["DailyRequestLimit"], out var daily) ? daily : 10000;
            var monthlyLimit = int.TryParse(configSection["MonthlyRequestLimit"], out var monthly) ? monthly : 300000;

            // Check if configuration with this name already exists
            var existingConfig = await firsApiKeyService.GetConfigurationByNameAsync(name, cancellationToken);
            
            FIRSApiConfiguration configuration;
            if (existingConfig != null)
            {
                _logger.LogInformation("On-Premise FIRS configuration already exists: {ConfigName}. Using existing configuration.", name);
                configuration = existingConfig;
            }
            else
            {
                _logger.LogInformation("Creating default On-Premise FIRS configuration: {ConfigName} (Requires KMPG approval)", name);
                configuration = await firsApiKeyService.CreateOnPremiseConfigurationAsync(
                    name,
                    description,
                    apiKey,
                    apiSecret,
                    cancellationToken);
            }

            // Set as default (but still requires approval to be active)
            await firsApiKeyService.SetDefaultConfigurationAsync(configuration.Id, cancellationToken);

            _logger.LogWarning("On-Premise FIRS configuration created but requires KMPG approval: {ConfigId}", configuration.Id);
            
            var approvalSection = _configuration.GetSection("FIRSApiConfiguration:KMPGApproval");
            var contactEmail = approvalSection["ContactEmail"] ?? "firs-approval@Aegis.com";
            var instructions = approvalSection["Instructions"] ?? "Submit FIRS API credentials to KMPG for approval";

            _logger.LogInformation("KMPG Approval Required:");
            _logger.LogInformation("Contact: {ContactEmail}", contactEmail);
            _logger.LogInformation("Instructions: {Instructions}", instructions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing On-Premise FIRS configuration");
            throw;
        }
    }
}