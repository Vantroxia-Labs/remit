using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.Authentication.Commands.Logout;

public class LogoutCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IJwtTokenService jwtTokenService,
    ITokenBlacklistService tokenBlacklistService,
    ILogger<LogoutCommandHandler> logger) : IRequestHandler<LogoutCommand, LogoutResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly ITokenBlacklistService _tokenBlacklistService = tokenBlacklistService;
    private readonly ILogger<LogoutCommandHandler> _logger = logger;

    public async Task<LogoutResult> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsUserAuthorized())
                return LogoutResult.AuthorizationError();

            // Blacklist the access token to prevent session replay attacks
            if (!string.IsNullOrWhiteSpace(request.AccessToken))
            {
                var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
                if (principal != null)
                {
                    var jti = principal.FindFirst("jti")?.Value;
                    var exp = principal.FindFirst("exp")?.Value;

                    if (!string.IsNullOrWhiteSpace(jti) && !string.IsNullOrWhiteSpace(exp))
                    {
                        // Convert Unix timestamp to DateTimeOffset
                        var expirationTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp));
                        await _tokenBlacklistService.BlacklistTokenAsync(jti, expirationTime, cancellationToken);
                        _logger.LogInformation("Access token with JTI {Jti} blacklisted during logout", jti);
                    }
                }
            }

            // Revoke refresh token
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                var refreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == _currentUser.UserId, cancellationToken);

                if (refreshToken != null && refreshToken.IsActive)
                {
                    refreshToken.Revoke(request.IpAddress, "User logout");
                }
            }

            // End all active sessions
            var activeSessions = await _context.UserSessions
                .Where(us => us.UserId == _currentUser.UserId && us.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var session in activeSessions)
            {
                session.End("User logout");
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} successfully logged out from IP {IpAddress}",
                _currentUser.UserId, request.IpAddress);

            return LogoutResult.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout Failed for user {UserId}", _currentUser.UserId);
            return LogoutResult.Failure($"Logout failed: {ex.Message}");
        }
    }

    private bool IsUserAuthorized() =>
        _currentUser.UserId.HasValue;
}