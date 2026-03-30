using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.UserManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static AegisEInvoicing.Domain.Entities.UserManagement.User;

namespace AegisEInvoicing.Application.Features.Authentication.Queries.ValidateToken;

public class ValidateTokenQueryHandler : IRequestHandler<ValidateTokenQuery, TokenValidationResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;

    public ValidateTokenQueryHandler(IApplicationDbContext context, IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<TokenValidationResult> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Use the JWT token service to validate the token
            if (!_jwtTokenService.ValidateToken(request.Token))
            {
                return new TokenValidationResult(false, null, "Invalid token", null);
            }

            // Extract claims from the token
            var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.Token);
            if (principal == null)
            {
                return new TokenValidationResult(false, null, "Could not extract claims from token", null);
            }

            // Extract user ID from token claims
            var userIdClaim = principal.FindFirst("userId")?.Value ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return new TokenValidationResult(false, null, "Invalid user ID in token", null);
            }

            // Extract expiration
            var expClaim = principal.FindFirst("exp")?.Value;
            DateTimeOffset? expiresAt = null;
            if (!string.IsNullOrEmpty(expClaim) && long.TryParse(expClaim, out var exp))
            {
                expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp);
            }

            // Check if token is expired
            if (expiresAt.HasValue && expiresAt.Value < DateTimeOffset.UtcNow)
            {
                return new TokenValidationResult(false, null, "Token has expired", expiresAt);
            }

            // =================================================================
            // SECURITY: Validate Session (ADDRESSES CONCURRENT LOGIN ISSUE)
            // =================================================================
            var sessionIdClaim = principal.FindFirst("sessionId")?.Value;
            if (!string.IsNullOrEmpty(sessionIdClaim) && Guid.TryParse(sessionIdClaim, out var sessionId))
            {
                var session = await _context.UserSessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

                if (session == null)
                {
                    return new TokenValidationResult(false, null, "Session not found", expiresAt);
                }

                if (!session.IsActive)
                {
                    // Session was terminated (e.g., by concurrent login enforcement)
                    return new TokenValidationResult(false, null, $"Session terminated: {session.EndReason ?? "Concurrent login detected"}", expiresAt);
                }
            }

            // Verify user still exists and is active
            var userExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == userId && u.Status == UserStatus.Active, cancellationToken);

            if (!userExists)
            {
                return new TokenValidationResult(false, null, "User not found or inactive", expiresAt);
            }

            // Check if token is revoked (if using access token blacklist - though usually RefreshTokens are checked)
            // Note: The original code was checking RefreshTokens table with access token, which might be incorrect unless access tokens are synced there.
            // Leaving as is but potentially ineffective for access tokens.
            var isRevoked = await _context.RefreshTokens
                .AsNoTracking()
                .AnyAsync(rt => rt.Token == request.Token && rt.IsRevoked, cancellationToken);

            if (isRevoked)
            {
                return new TokenValidationResult(false, null, "Token has been revoked", expiresAt);
            }

            return new TokenValidationResult(true, userId, null, expiresAt);
        }
        catch (Exception ex)
        {
            return new TokenValidationResult(false, null, $"Token validation error: {ex.Message}", null);
        }
    }
}