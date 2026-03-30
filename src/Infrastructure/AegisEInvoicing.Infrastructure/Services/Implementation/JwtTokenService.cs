using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Entities.UserManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AegisEInvoicing.Infrastructure.Services.Implementation;

/// <summary>
/// Enterprise-level JWT token service with security best practices
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IEncryptionService _encryptionService;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(
        int.TryParse(_configuration["JWT:AccessTokenLifetimeMinutes"], out var minutes) ? minutes : 15);
    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(
        int.TryParse(_configuration["JWT:RefreshTokenLifetimeDays"], out var days) ? days : 7);

    public JwtTokenService(IConfiguration configuration, IEncryptionService encryptionService)
    {
        _configuration = configuration;
        _encryptionService = encryptionService;
        _secretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured");
        _issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer not configured");
        _audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience not configured");

        // Validate secret key strength (minimum 256 bits for HS256)
        if (_secretKey.Length < 32)
            throw new InvalidOperationException("JWT secret key must be at least 256 bits (32 characters) long");
    }

    public string GenerateAccessToken(User user, IEnumerable<string> permissions, IEnumerable<string> roles, Guid? sessionId = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);        

        var claims = new List<Claim>
        {
            // Standard claims with PII (token will be encrypted)
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),

            // Authorization context claims (required for access control)
            new("businessId", user.BusinessId?.ToString() ?? ""),
            new("branchId", user.BranchId?.ToString() ?? ""),
            new("userStatus", user.Status.ToString()),
            new("emailVerified", user.IsEmailVerified.ToString()),
            new("mustChangePassword", user.MustChangePassword.ToString()),
            new("isBusinessLevel", (!user.BranchId.HasValue).ToString()),
            new("isBranchLevel", user.BranchId.HasValue.ToString()),

            // Aegis authorization context (role needed for access control)
            new("isAegisUser", user.IsAegisUser.ToString()),
            new("AegisRole", user.AegisRole?.ToString() ?? "")
        };

        // Add sessionId claim if provided (for concurrent login enforcement)
        if (sessionId.HasValue)
        {
            claims.Add(new Claim("sessionId", sessionId.Value.ToString()));
        }

        // Add SubscriptionTier and DeploymentMode to token for authorization
        if (user.Business is not null)
        {
            // Add DeploymentMode (Cloud or OnPremise)
            claims.Add(new Claim("DeploymentMode", user.Business.DeploymentMode.ToString()));

            // Add SubscriptionTier if user has an active subscription
            if (user.Business.Subscription?.PlatformSubscription is not null)
            {
                claims.Add(new Claim("SubscriptionTier", user.Business.Subscription.PlatformSubscription.Tier.ToString()));
            }
        }

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add permission claims
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(AccessTokenLifetime),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            NotBefore = DateTime.UtcNow, // Token not valid before now
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Generate cryptographically secure random token
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[64];
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // Don't validate lifetime for refresh scenarios
            ValidateIssuerSigningKey = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            
            // Verify it's a JWT token with the correct algorithm
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public bool ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GenerateEncryptedAccessTokenAsync(User user, IEnumerable<string> permissions, IEnumerable<string> roles, Guid? sessionId = null)
    {
        var jwt = GenerateAccessToken(user, permissions, roles, sessionId);
        return await _encryptionService.EncryptAsync(jwt);
    }

    public async Task<string> DecryptAccessTokenAsync(string encryptedToken)
    {
        return await _encryptionService.DecryptAsync(encryptedToken);
    }
}