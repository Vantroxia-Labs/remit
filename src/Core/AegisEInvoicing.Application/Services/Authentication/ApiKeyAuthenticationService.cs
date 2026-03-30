using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Entities.UserManagement;

namespace AegisEInvoicing.Application.Services.Authentication;

public interface IApiKeyAuthenticationService
{
    Task<ApiKeyValidationResult> ValidateApiKeyAsync(string apiKey);
    Task<string> GenerateApiKeyAsync(Guid businessId);
    Task<bool> RevokeApiKeyAsync(string apiKey);
    Task<bool> IsSubscriptionValidForApiAccessAsync(Guid businessId);
}

public class ApiKeyAuthenticationService : IApiKeyAuthenticationService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ApiKeyAuthenticationService> _logger;
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationService(
        IApplicationDbContext dbContext,
        ILogger<ApiKeyAuthenticationService> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ApiKeyValidationResult> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ApiKeyValidationResult { IsValid = false, Error = "API key is required" };
        }

        try
        {
            // Find business by API key
            var business = await _dbContext.Businesses
                .Include(b => b.Subscription)
                    .ThenInclude(s => s!.PlatformSubscription)
                .FirstOrDefaultAsync(b => b.ApiKey == apiKey && b.IsApiKeyActive);

            if (business == null)
            {
                _logger.LogWarning("Invalid API key attempted: {ApiKey}", apiKey.Substring(0, 10) + "...");
                return new ApiKeyValidationResult { IsValid = false, Error = "Invalid API key" };
            }

            if (business.AdminUserId is null || business.AdminUserId == Guid.Empty)
            {
                return new ApiKeyValidationResult
                {
                    IsValid = false,
                    Error = "Business has no active admin user",
                    BusinessId = business.Id
                };
            }

            // Check if business is active
            if (business.Status != AegisEInvoicing.Domain.Enums.BusinessStatus.Active)
            {
                return new ApiKeyValidationResult 
                { 
                    IsValid = false, 
                    Error = "Business account is not active",
                    BusinessId = business.Id
                };
            }

            // Check subscription
            if (business.Subscription == null || !business.Subscription.IsActive())
            {
                return new ApiKeyValidationResult 
                { 
                    IsValid = false, 
                    Error = "Subscription is not active or has expired",
                    BusinessId = business.Id
                };
            }

            // Check if subscription tier allows API access
            if (business.Subscription.PlatformSubscription == null)
            {
                return new ApiKeyValidationResult 
                { 
                    IsValid = false, 
                    Error = "Subscription plan information is not available",
                    BusinessId = business.Id
                };
            }
            
            if (business.Subscription.PlatformSubscription.Tier != SubscriptionTier.ApiOnly &&
                business.Subscription.PlatformSubscription.Tier != SubscriptionTier.SaaS)
            {
                return new ApiKeyValidationResult 
                { 
                    IsValid = false, 
                    Error = "Your subscription plan does not include API access. Please upgrade to API-Only or SaaS plan.",
                    BusinessId = business.Id
                };
            }

            // Create claims for the authenticated API client
            var claims = new List<Claim>
            {
                // Standard identity claims
                new Claim(ClaimTypes.Name, business.Name), // Used by ICurrentUserService.UserName
                new Claim(ClaimTypes.Email, business.ContactEmail), // Used by ICurrentUserService.Email
                new Claim(ClaimTypes.Role, "ApiClient"), // Used by ICurrentUserService.Roles

                // Business-specific claims (lowercase for ICurrentUserService)
                new Claim("businessId", business.Id.ToString()), // Used by ICurrentUserService.BusinessId
                new Claim("BusinessId", business.Id.ToString()), // For consistency
                new Claim("BusinessName", business.Name),

                // Level claims (for ICurrentUserService.IsBusinessLevel/IsBranchLevel)
                new Claim("isBusinessLevel", "true"),
                new Claim("isBranchLevel", "false"),

                // Aegis claims (for ICurrentUserService Aegis properties)
                new Claim("isAegisUser", "false"), // API clients are not Aegis users

                // Subscription info
                new Claim("SubscriptionTier", business.Subscription.PlatformSubscription.Tier.ToString()),

                // API Key info (truncated for security)
                new Claim("ApiKey", apiKey.Substring(0, 10) + "..."),

                // Permissions for API clients
                new Claim("permission", "api.read"),
                new Claim("permission", "api.write"),
                new Claim("permission", "invoice.create"),
                new Claim("permission", "invoice.read"),
                new Claim("permission", "invoice.update"),
                new Claim("permission", "invoice.transmit")
            };

            if (business.AdminUserId is not null)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, business.AdminUserId.ToString()!));
            }

            _logger.LogInformation("API key validated successfully for business: {BusinessId}", business.Id);

            return new ApiKeyValidationResult
            {
                IsValid = true,
                BusinessId = business.Id,
                BusinessName = business.Name,
                SubscriptionTier = business.Subscription.PlatformSubscription.Tier,
                Claims = claims,
                RateLimitTier = GetRateLimitTier(business.Subscription.PlatformSubscription.Tier)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return new ApiKeyValidationResult { IsValid = false, Error = "An error occurred during validation" };
        }
    }

    public async Task<string> GenerateApiKeyAsync(Guid businessId)
    {
        var business = await _dbContext.Businesses
            .Include(b => b.Subscription)
                .ThenInclude(s => s!.PlatformSubscription)
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business == null)
        {
            throw new InvalidOperationException("Business not found");
        }

        // Check if business already has an active API key
        if (business.HasValidApiKey())
        {
            throw new InvalidOperationException("Business already has an active API key. Revoke it first before generating a new one.");
        }

        // Generate a secure API key
        var apiKey = GenerateSecureApiKey(businessId);

        // Store the API key using the domain method
        business.SetApiKey(apiKey, businessId); // Using businessId as the generator for now
        _dbContext.Businesses.Update(business);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("API key generated for business: {BusinessId}", businessId);

        return apiKey;
    }

    public async Task<bool> RevokeApiKeyAsync(string apiKey)
    {
        var business = await _dbContext.Businesses
            .FirstOrDefaultAsync(b => b.ApiKey == apiKey);

        if (business == null)
        {
            return false;
        }

        // Revoke the API key using the domain method
        business.RevokeApiKey(business.Id); // Using business.Id as the revoker for now
        _dbContext.Businesses.Update(business);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("API key revoked for business: {BusinessId}", business.Id);

        return true;
    }

    public async Task<bool> IsSubscriptionValidForApiAccessAsync(Guid businessId)
    {
        var business = await _dbContext.Businesses
            .Include(b => b.Subscription)
                .ThenInclude(s => s!.PlatformSubscription)
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business?.Subscription == null)
        {
            return false;
        }

        // Check if subscription is active and has API access
        return business.Subscription.IsActive() && 
               business.Subscription.PlatformSubscription != null &&
               (business.Subscription.PlatformSubscription.Tier == SubscriptionTier.ApiOnly ||
                business.Subscription.PlatformSubscription.Tier == SubscriptionTier.SaaS);
    }

    private string GenerateSecureApiKey(Guid businessId)
    {
        // Create a unique key combining business ID, timestamp, and random data
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var keyData = $"{businessId}|{timestamp}|{Convert.ToBase64String(randomBytes)}";
        
        // Hash the key data
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyData));
            var apiKey = $"sk_live_{Convert.ToBase64String(hashBytes).Replace("/", "_").Replace("+", "-").TrimEnd('=')}";
            
            // Ensure consistent length
            if (apiKey.Length > 64)
            {
                apiKey = apiKey.Substring(0, 64);
            }

            return apiKey;
        }
    }

    private string GetRateLimitTier(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.ApiOnly => "ApiOnly",
            SubscriptionTier.SaaS => "SaaS",
            SubscriptionTier.SFTP => "SFTP",
            _ => "ApiOnly"
        };
    }
}

public class ApiKeyValidationResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public Guid? BusinessId { get; set; }
    public string? BusinessName { get; set; }
    public SubscriptionTier? SubscriptionTier { get; set; }
    public List<Claim> Claims { get; set; } = new();
    public string? RateLimitTier { get; set; }
}