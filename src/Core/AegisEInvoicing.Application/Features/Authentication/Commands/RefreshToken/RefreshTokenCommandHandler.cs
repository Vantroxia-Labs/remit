using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.Authentication.Commands.Login;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IApplicationDbContext context,
    IJwtTokenService jwtTokenService,
    ILogger<RefreshTokenCommandHandler> logger) : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger = logger;

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var refreshToken = await GetActiveRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (refreshToken is null)
                return RefreshTokenResult.AuthorizationError("Invalid or expired refresh token");

            var userValidation = await ValidateUserAccountAsync(refreshToken, request.IpAddress, cancellationToken);
            if (!userValidation.IsValid)
                return userValidation.Result;

            return await GenerateNewTokensAsync(refreshToken, request.IpAddress, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh Token Failed");
            return RefreshTokenResult.Failure($"Token refresh failed: {ex.Message}");
        }
    }

    private async Task<Domain.Entities.UserManagement.RefreshToken?> GetActiveRefreshTokenAsync(string token, CancellationToken cancellationToken)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.Business!)
                    .ThenInclude(b => b.Subscriptions)
                        .ThenInclude(s => s.PlatformSubscription)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        return refreshToken?.IsActive == true ? refreshToken : null;
    }

    private async Task<ValidationResult> ValidateUserAccountAsync(Domain.Entities.UserManagement.RefreshToken refreshToken, string ipAddress, CancellationToken cancellationToken)
    {
        var user = refreshToken.User;

        if (user.CanLogin())
            return ValidationResult.Valid();

        refreshToken.Revoke(ipAddress, "User account no longer active");
        await _context.SaveChangesAsync(cancellationToken);

        return ValidationResult.Invalid(
            RefreshTokenResult.AuthorizationError("User account is not active"));
    }

    private async Task<RefreshTokenResult> GenerateNewTokensAsync(Domain.Entities.UserManagement.RefreshToken currentRefreshToken, string ipAddress, CancellationToken cancellationToken)
    {
        var user = currentRefreshToken.User;

        // Get user roles and permissions
        var (roles, permissions) = await GetUserRolesAndPermissionsAsync(user.Id, cancellationToken);

        // Find active session for this user/IP if possible, or create new one?
        // For refresh token, we typically want to maintain the existing session or create a new linked one.
        // However, since we don't have the old access token here, we can't easily find the old session ID.
        // But concurrent login limits might kill old sessions.
        
        // Strategy: Create a new session ID for the refreshed token.
        // This effectively rotates the session ID on refresh, which is good security.
        // But we should probably check if an active session exists for this user/IP to reuse or update it?
        
        // Simple approach: Generate new session ID.
        // NOTE: If we don't create a new UserSession record in DB, this ID won't validation!
        // We MUST verify if Refresh Token flow should validate session or just user.
        
        // If we want to enforce session limits, even Refresh Token flow must respect it.
        // However, Refresh Token flow usually implies an existing valid session (that just needs new access token).
        
        // Let's assume we need to link to an active session.
        // We can find the most recent active session for this user and IP.
        var activeSession = await _context.UserSessions
            .Where(s => s.UserId == user.Id && s.IsActive && s.IpAddress == ipAddress)
            .OrderByDescending(s => s.LastActivityAt)
            .FirstOrDefaultAsync(cancellationToken);

        Guid sessionId;
        if (activeSession != null)
        {
            sessionId = activeSession.Id;
            activeSession.UpdateActivity(); // Extend session
        }
        else
        {
            // If no active session found (maybe expired/cleaned up), create new one?
            // Or just generate ID and let loose? (If it's loose, it will fail validation if validation checks DB)
            // Ideally we should fail if no session, but RefreshToken might be valid while session expired?
            // Let's create a new session to be safe and ensure continuity.
            sessionId = Guid.CreateVersion7();
            var newSession = Domain.Entities.UserManagement.UserSession.Create(user.Id, ipAddress, "Refreshed Session");
            newSession.Id = sessionId;
            await _context.UserSessions.AddAsync(newSession, cancellationToken);
        }

        // Generate new encrypted access token with session ID
        var newAccessToken = await _jwtTokenService.GenerateEncryptedAccessTokenAsync(user, permissions, roles, sessionId);
        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        // Create new refresh token
        var newRefreshToken = Domain.Entities.UserManagement.RefreshToken.Create(
            user.Id,
            newRefreshTokenValue,
            DateTimeOffset.UtcNow.Add(_jwtTokenService.RefreshTokenLifetime),
            ipAddress);

        // Revoke old token and save new one
        currentRefreshToken.Revoke(ipAddress, "Token rotated", newRefreshTokenValue);
        await _context.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Build claims DTO for frontend consumption
        var claims = new TokenClaimsDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Roles = roles,
            Permissions = permissions,
            BusinessId = user.BusinessId,
            BranchId = user.BranchId,
            IsAegisUser = user.IsAegisUser,
            AegisRole = user.AegisRole?.ToString(),
            SubscriptionTier = user.Business?.GetPrimarySubscription()?.PlatformSubscription?.Tier.ToString()
        };

        return RefreshTokenResult.Successful(
            newAccessToken,
            newRefreshTokenValue,
            DateTimeOffset.UtcNow.Add(_jwtTokenService.AccessTokenLifetime),
            claims);
    }

    private async Task<(List<string> roles, List<string> permissions)> GetUserRolesAndPermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeRoles = await _context.UserRoleAssignments
            .Where(ura => ura.UserId == userId && ura.IsActive)
            .Include(ura => ura.PlatformRole)
            .Select(ura => ura.PlatformRole)
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        var roles = activeRoles.Select(r => r.Name).ToList();
        var permissions = activeRoles.SelectMany(r => r.Permissions).Distinct().ToList();

        return (roles, permissions);
    }

    // Helper class for validation results
    private class ValidationResult
    {
        public bool IsValid { get; private set; }
        public RefreshTokenResult Result { get; private set; }

        private ValidationResult(bool isValid, RefreshTokenResult? result = null)
        {
            IsValid = isValid;
            Result = (result ?? RefreshTokenResult.Failure("Validation failed"));
        }

        public static ValidationResult Valid() => new(true);
        public static ValidationResult Invalid(RefreshTokenResult result) => new(false, result);
    }
}