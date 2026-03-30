using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using System.Diagnostics;
using AegisEInvoicing.Infrastructure.Services.Interfaces;
using AegisEInvoicing.Infrastructure.Models;

namespace AegisEInvoicing.Infrastructure.Services.Implementation;

public class LicenseValidationService(
    IApplicationDbContext context,
    IHostApplicationLifetime applicationLifetime,
    IEncryptionService encryptionService,
    ILogger<LicenseValidationService> logger,
    IConfiguration configuration,
    ITelemetryService? telemetryService = null) : ILicenseValidationService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IHostApplicationLifetime _applicationLifetime = applicationLifetime;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly ILogger<LicenseValidationService> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly ITelemetryService? _telemetryService = telemetryService;
    private static readonly SemaphoreSlim _licenseCheckSemaphore = new(1, 1);
    private static DateTime _lastCheckTime = DateTime.MinValue;
    private static bool _lastValidationResult = false;

    public async Task<bool> ValidateLicenseAsync(CancellationToken cancellationToken = default)
    {
        await _licenseCheckSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Check cache (validate every minute)
            if (_lastCheckTime.AddMinutes(1) > DateTime.UtcNow)
            {
                return _lastValidationResult;
            }

            var systemConfig = await _context.SystemConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (systemConfig == null)
            {
                _logger.LogWarning("System configuration not found. License validation failed.");
                _lastValidationResult = false;
                return false;
            }

            // For SaaS mode, always valid (subscription checked separately)
            if (systemConfig.DeploymentMode == DeploymentMode.Cloud)
            {
                _lastValidationResult = true;
                _lastCheckTime = DateTime.UtcNow;
                return true;
            }

            // For On-Premise, validate license
            if (systemConfig.DeploymentMode == DeploymentMode.OnPremise)
            {
                var isValid = await ValidateOnPremiseLicenseAsync(systemConfig, cancellationToken);
                _lastValidationResult = isValid;
                _lastCheckTime = DateTime.UtcNow;
                
                // Track license validation - get the actual business ID for on-premise
                try
                {
                    var business = await _context.Businesses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(b => b.DeploymentMode == DeploymentMode.OnPremise && !b.IsDeleted, cancellationToken);
                    
                    if (business != null)
                    {
                        _telemetryService?.TrackLicenseValidated(business.Id, isValid, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to track license validation telemetry");
                }
                
                if (!isValid)
                {
                    _logger.LogCritical("License validation failed for on-premise deployment");
                    ShutdownApplication("License expired or invalid");
                }
                
                return isValid;
            }

            return false;
        }
        finally
        {
            _licenseCheckSemaphore.Release();
        }
    }

    private async Task<bool> ValidateOnPremiseLicenseAsync(SystemConfiguration config, CancellationToken cancellationToken)
    {
        try
        {
            // Check license expiry
            if (!config.LicenseExpiryDate.HasValue || config.LicenseExpiryDate.Value <= DateTimeOffset.UtcNow)
            {
                _logger.LogError("License has expired. Expiry date: {ExpiryDate}", config.LicenseExpiryDate);
                return false;
            }

            // Validate license key format and signature
            if (string.IsNullOrEmpty(config.LicenseKey))
            {
                _logger.LogError("License key is missing");
                return false;
            }

            // Check for tampered license file if exists
            var licenseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.lic");
            if (File.Exists(licenseFilePath))
            {
                var fileContent = await File.ReadAllTextAsync(licenseFilePath, cancellationToken);
                var isFileValid = ValidateLicenseFile(fileContent, config.LicenseKey, cancellationToken);
                
                if (!isFileValid)
                {
                    _logger.LogCritical("License file has been tampered with or is invalid");
                    return false;
                }
            }

            // Additional hardware fingerprint validation (for stricter enforcement)
            var hardwareId = GetHardwareFingerprint();
            var expectedHardwareId = ExtractHardwareIdFromLicense(config.LicenseKey);
            
            if (!string.IsNullOrEmpty(expectedHardwareId) && hardwareId != expectedHardwareId)
            {
                _logger.LogError("License is not valid for this hardware");
                return false;
            }

            _logger.LogInformation("License validation successful. Expires on: {ExpiryDate}", config.LicenseExpiryDate);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during license validation");
            return false;
        }
    }

    private bool ValidateLicenseFile(string fileContent, string licenseKey, CancellationToken cancellationToken)
    {
        try
        {
            var licenseData = JsonSerializer.Deserialize<LicenseFileData>(fileContent);
            if (licenseData == null)
                return false;

            // Verify signature
            var dataToVerify = $"{licenseData.OrganizationName}|{licenseData.ExpiryDate:O}|{licenseData.LicenseKey}";
            var computedHash = ComputeHash(dataToVerify);

            return computedHash == licenseData.Signature;
        }
        catch
        {
            return false;
        }
    }

    private static string GetHardwareFingerprint()
    {
        try
        {
            var machineName = Environment.MachineName;
            var processorCount = Environment.ProcessorCount;
            var osVersion = Environment.OSVersion.ToString();
            
            var fingerprint = $"{machineName}|{processorCount}|{osVersion}";
            return ComputeHash(fingerprint);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ExtractHardwareIdFromLicense(string licenseKey)
    {
        try
        {
            // License key format: BASE64(organizationId|hardwareId|expiryDate|signature)
            var decodedBytes = Convert.FromBase64String(licenseKey);
            var decodedString = Encoding.UTF8.GetString(decodedBytes);
            var parts = decodedString.Split('|');
            
            if (parts.Length >= 2)
                return parts[1];
            
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

    public async Task<LicenseInfo> GetLicenseInfoAsync(CancellationToken cancellationToken = default)
    {
        var systemConfig = await _context.SystemConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (systemConfig == null || systemConfig.DeploymentMode != DeploymentMode.OnPremise)
        {
            return new LicenseInfo { IsValid = false };
        }

        var isValid = systemConfig.IsLicenseValid();
        var daysRemaining = 0;
        
        if (systemConfig.LicenseExpiryDate.HasValue)
        {
            daysRemaining = Math.Max(0, (systemConfig.LicenseExpiryDate.Value - DateTimeOffset.UtcNow).Days);
        }

        return new LicenseInfo
        {
            IsValid = isValid,
            ExpiryDate = systemConfig.LicenseExpiryDate,
            DaysRemaining = daysRemaining,
            OrganizationName = systemConfig.OrganizationName,
            LicenseType = "On-Premise"
        };
    }

    public bool IsLicenseValid()
    {
        return _lastValidationResult;
    }

    public void ShutdownApplication(string reason)
    {
        _logger.LogCritical("Application shutdown initiated: {Reason}", reason);
        
        // Log to Event Log on Windows
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var eventLog = new EventLog("Application");
                eventLog.Source = "AegisEInvoicing";
                eventLog.WriteEntry($"Application shutdown: {reason}", EventLogEntryType.Error);
            }
            catch { }
        }

        // Trigger graceful shutdown
        _applicationLifetime.StopApplication();
    }
}