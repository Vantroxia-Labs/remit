using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.UserManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.Authentication.Commands.Login;

public class LoginCommandHandler(
    IApplicationDbContext context,
    IJwtTokenService jwtTokenService,
    ISessionManagementService sessionManagementService,
    ILogger<LoginCommandHandler> logger) : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly ILogger<LoginCommandHandler> _logger = logger;
    private readonly IApplicationDbContext _context = context;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly ISessionManagementService _sessionManagementService = sessionManagementService;

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await GetUserWithDetailsAsync(request.Email, cancellationToken);
            if (user is null)
                return LoginResult.Failure();

            var accountValidation = ValidateUserAccount(user);
            if (!accountValidation.IsValid)
                return accountValidation.Result;

            var passwordValidation = await ValidatePasswordAsync(user, request.Password, request.IpAddress, cancellationToken);
            if (passwordValidation.IsValid)
                return passwordValidation.Result;

            var subscriptionValidation = ValidateBusinessSubscription(user);
            if (!subscriptionValidation.IsValid)
                return subscriptionValidation.Result;

            return await CreateSuccessfulLoginAsync(user, request.IpAddress, request.UserAgent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login Failed for {Email}", request.Email);
            return LoginResult.Failure($"Login failed: {ex.Message}");
        }
    }

    private async Task<User?> GetUserWithDetailsAsync(string email, CancellationToken cancellationToken)
    {
        return await _context.Users
            .Include(u => u.RoleAssignments)
                .ThenInclude(ra => ra.PlatformRole)
            .Include(u => u.Business!)
                .ThenInclude(b => b.Subscriptions)
                .ThenInclude(s => s.PlatformSubscription)
            .SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    private static ValidationResult ValidateUserAccount(User user)
    {
        if (!user.CanLogin())
        {
            if (user.IsLocked())
            {
                var lockMessage = $"Account is locked until {user.LockedOutUntil:yyyy-MM-dd HH:mm} UTC";
                return ValidationResult.Invalid(LoginResult.Locked(lockMessage));
            }

            var statusMessage = $"Account is {user.Status}. Please contact your administrator.";
            return ValidationResult.Invalid(LoginResult.Locked(statusMessage));
        }

        return ValidationResult.Valid();
    }

    private async Task<ValidationResult> ValidatePasswordAsync(User user, string password, string ipAddress, CancellationToken cancellationToken)
    {
        if (user.PasswordHash.Verify(password))
            return ValidationResult.Valid();

        user.RecordFailedLogin(ipAddress);
        await _context.SaveChangesAsync(cancellationToken);

        if (user.IsLocked())
        {
            var lockMessage = $"Invalid password. Account locked until {user.LockedOutUntil:yyyy-MM-dd HH:mm} UTC";
            return ValidationResult.Invalid(LoginResult.Locked(lockMessage));
        }

        return ValidationResult.Invalid(LoginResult.Failure());
    }

    private static ValidationResult ValidateBusinessSubscription(User user)
    {
        if (user.BusinessId.HasValue &&
            user.Business?.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active) != true)
        {
            return ValidationResult.Invalid(LoginResult.Locked("Account is inactive"));
        }

        return ValidationResult.Valid();
    }

    private async Task<LoginResult> CreateSuccessfulLoginAsync(User user, string ipAddress, string userAgent, CancellationToken cancellationToken)
    {
        // =================================================================
        // SECURITY: Enforce Concurrent Session Limits
        // =================================================================
        // Addresses VAPT finding: Concurrent login enabled
        // This terminates oldest sessions when MaxConcurrentSessions limit is exceeded
        // Configuration: appsettings.json -> SessionManagement:MaxConcurrentSessions (default: 1)
        int terminatedSessions;
        if (_sessionManagementService.MaxConcurrentSessions == 1)
        {
            // Strict single-session behaviour: terminate all other active sessions
            terminatedSessions = await _sessionManagementService.TerminateOtherSessionsAsync(user.Id, null, cancellationToken);
            if (terminatedSessions > 0)
            {
                _logger.LogWarning(
                    "CONCURRENT LOGIN DETECTED: Terminated {Count} previous session(s) for user {UserId} ({Email}) " +
                    "due to single-session enforcement. New login from IP: {IpAddress}, User-Agent: {UserAgent}",
                    terminatedSessions, user.Id, user.Email, ipAddress, userAgent);
            }
        }
        else
        {
            // General enforcement: only terminate the minimum number needed
            terminatedSessions = await _sessionManagementService.EnforceSessionLimitAsync(user.Id, cancellationToken);
            if (terminatedSessions > 0)
            {
                _logger.LogWarning(
                    "CONCURRENT LOGIN DETECTED: Terminated {Count} previous session(s) for user {UserId} ({Email}) " +
                    "due to concurrent session limit enforcement. New login from IP: {IpAddress}, User-Agent: {UserAgent}",
                    terminatedSessions, user.Id, user.Email, ipAddress, userAgent);
            }
        }

        // Generate Session ID upfront
        var sessionId = Guid.CreateVersion7();

        // Get user roles and permissions from already-loaded navigation properties (no extra DB round-trip)
        var (roles, permissions) = GetUserRolesAndPermissions(user);

        // Generate encrypted access token (entire JWT is AES encrypted)
        // Pass sessionId to bind token to session
        var accessToken = await _jwtTokenService.GenerateEncryptedAccessTokenAsync(user, permissions, roles, sessionId);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        var refreshToken = Domain.Entities.UserManagement.RefreshToken.Create(
            user.Id,
            refreshTokenValue,
            DateTimeOffset.UtcNow.Add(_jwtTokenService.RefreshTokenLifetime),
            ipAddress);

        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);

        user.RecordSuccessfulLogin(ipAddress);

        var session = UserSession.Create(user.Id, ipAddress, userAgent);
        // FORCE session ID to match token claim
        session.Id = sessionId;
        
        await _context.UserSessions.AddAsync(session, cancellationToken);

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
            SubscriptionTier = user.Business?.GetPrimarySubscription()?.PlatformSubscription?.Tier.ToString(),
            DeploymentMode = user.Business?.DeploymentMode.ToString()
        };

        // Return login result with session termination information
        // The terminatedSessions count triggers a warning message in the response
        return LoginResult.Successful(
            accessToken,
            refreshTokenValue,
            user.Id,
            user.BusinessId,
            user.MustChangePassword,
            DateTimeOffset.UtcNow.Add(_jwtTokenService.AccessTokenLifetime),
            claims,
            terminatedSessions); // Pass terminated session count for user notification
    }

    private static (List<string> roles, List<string> permissions) GetUserRolesAndPermissions(User user)
    {
        var activeRoles = user.RoleAssignments
            .Where(ra => ra.IsActive && ra.PlatformRole?.IsActive == true)
            .Select(ra => ra.PlatformRole!)
            .ToList();

        var roles = activeRoles.Select(r => r.Name).ToList();
        var permissions = activeRoles.SelectMany(r => r.Permissions).Distinct().ToList();

        return (roles, permissions);
    }

    private class ValidationResult
    {
        public bool IsValid { get; private set; }
        public LoginResult Result { get; private set; }

        private ValidationResult(bool isValid, LoginResult? result = null)
        {
            IsValid = isValid;
            Result = result ?? LoginResult.Failure();
        }

        public static ValidationResult Valid() => new(true);
        public static ValidationResult Invalid(LoginResult result) => new(false, result);
    }
}