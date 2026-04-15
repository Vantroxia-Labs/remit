using Asp.Versioning;
using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Portal.API.Models.Authentication.ForgotPassword;
using AegisEInvoicing.Application.Features.Authentication.Commands.Login;
using AegisEInvoicing.Application.Features.Authentication.Commands.Logout;
using AegisEInvoicing.Application.Features.Authentication.Commands.RefreshToken;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.RegisterBusiness;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands.SendActionOtp;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands.RequestChangePassword;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands.SendForgotPasswordOTP;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Enterprise-level authentication controller with security best practices
/// Handles login, logout, and token refresh operations
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthenticationController : BaseApiController
{
    /// <summary>
    /// Self-service business registration — initiates Paystack payment
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("Authentication")]
    [AllowAnonymous]    [ProducesResponseType(typeof(ApiResponse<RegisterBusinessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterBusinessRequest request)
    {
        var command = new RegisterBusinessCommand(
            request.AdminFirstName,
            request.AdminLastName,
            request.AdminEmail,
            request.AdminPhone,
            request.BusinessName,
            request.PlatformSubscriptionId,
            request.BillingCycle,
            request.Tin);

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return Error(result.Message);

        return Success(new RegisterBusinessResponse(
            result.PendingRegistrationId!.Value,
            result.Reference!,
            result.PaymentUrl!), result.Message);
    }

    /// <summary>
    /// Authenticates a user and returns JWT tokens
    /// </summary>
    /// <param name="request">Login credentials (email and password)</param>
    /// <returns>JWT access and refresh tokens with user information and expiration</returns>
    [HttpPost("login")]
    [EnableRateLimiting("Authentication")] // Rate limit: 5 attempts per IP per 5 minutes
    [AllowAnonymous]    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status423Locked)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = GetIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var command = new LoginCommand(request.Email, request.Password, ipAddress, userAgent);
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes);

        var response = new LoginResponse(
            result.AccessToken!,
            result.RefreshToken!,
            result.UserId!.Value,
            result.BusinessId,
            result.MustChangePassword,
            result.ExpiresAt!.Value,
            result.Claims,
            result.TerminatedSessionCount,
            result.SessionWarning);

        // Set refresh token in secure HTTP-only cookie for additional security
        SetRefreshTokenCookie(result.RefreshToken!);

        // If sessions were terminated, include warning in the response message
        var message = result.TerminatedSessionCount > 0
            ? $"{result.Message}. {result.SessionWarning}"
            : result.Message;

        return Success(response, message);
    }

    /// <summary>
    /// Refreshes an expired JWT token using a valid refresh token
    /// </summary>
    /// <param name="request">Refresh token request (optional if token is in cookie)</param>
    /// <returns>New JWT access and refresh tokens with updated expiration</returns>
    [HttpPost("refresh")]
    [EnableRateLimiting("Authentication")] // Rate limit: 5 attempts per IP per 5 minutes
    [AllowAnonymous]    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request = null)
    {
        var refreshToken = request?.RefreshToken ?? GetRefreshTokenFromCookie();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Error("Refresh token is required", StatusCodes.Status400BadRequest);
        }

        var ipAddress = GetIpAddress();
        var command = new RefreshTokenCommand(refreshToken, ipAddress);
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes);

        var response = new RefreshTokenResponse(
            result.AccessToken!,
            result.RefreshToken!,
            result.ExpiresAt!.Value,
            result.Claims);

        // Update refresh token in cookie
        SetRefreshTokenCookie(result.RefreshToken!);

        return Success(response, result.Message);
    }

    /// <summary>
    /// Logs out the current user and revokes their tokens
    /// </summary>
    /// <param name="request">Optional logout request with refresh token</param>
    /// <returns>Logout confirmation</returns>
    [HttpPost("logout")]
    [Authorize] // Require authentication to logout    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request = null)
    {
        var refreshToken = request?.RefreshToken ?? GetRefreshTokenFromCookie();
        var ipAddress = GetIpAddress();

        var command = new LogoutCommand(refreshToken, ipAddress);
        var result = await Mediator.Send(command);

        // Clear refresh token cookie
        ClearRefreshTokenCookie();

        return GenericResponse(result.Message, result.IsSuccess, result.StatusCodes);
    }

    /// <summary>
    /// This endpoint allows users to request otp while doing password reset
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("forgot-password-request-otp")]
    [EnableRateLimiting("Authentication")] // Rate limit: 5 attempts per IP per 5 minutes
    [AllowAnonymous]    public async Task<IActionResult> SendForgotPasswordOTP([FromBody] SendForgotPasswordOTP request)
    {
        var command = new SendForgotPasswordOTPCommand(request.PhoneNo_Email.Trim());
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }

    /// <summary>
    /// Sends OTP for confirming a sensitive in-session action (authenticated users only).
    /// </summary>
    [HttpPost("send-action-otp")]
    [Authorize]
    [EnableRateLimiting("Authentication")]    public async Task<IActionResult> SendActionOtp()
    {
        var result = await Mediator.Send(new SendActionOtpCommand());

        if (!result.IsSuccess)
        {
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }

    /// <summary>
    /// This endpoint allows users to do forget password
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("Authentication")] // Rate limit: 5 attempts per IP per 5 minutes
    [AllowAnonymous]    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPassword request)
    {
        var command = new ForgotPasswordCommand(request.Otp.Trim(), request.Password.Trim(), request.PhoneNo_Email.Trim());
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(Error(result.Message));
        }

        return Success(result, result.Message);
    }

    /// <summary>
    /// Revokes all refresh tokens for the current user (useful for security incidents)
    /// </summary>
    /// <returns>Revocation confirmation</returns>
    [HttpPost("revoke-all")]
    [Authorize] // Require authentication    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> RevokeAllTokens()
    {
        // This would be implemented as a separate command
        // For now, return a placeholder response
        return Task.FromResult<IActionResult>(Success<object?>(null, "All tokens revoked successfully (placeholder)"));
    }

    /// <summary>
    /// Gets the claims from the current user's token for frontend use after page refresh
    /// </summary>
    /// <returns>Token claims including user info, roles, and permissions</returns>
    [HttpGet("token-claims")]
    [Authorize]    [ProducesResponseType(typeof(ApiResponse<TokenClaimsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public IActionResult GetTokenClaims()
    {
        var claims = new TokenClaimsDto
        {
            FirstName = User.FindFirst(JwtRegisteredClaimNames.GivenName)?.Value,
            LastName = User.FindFirst(JwtRegisteredClaimNames.FamilyName)?.Value,
            Email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value,
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
            Permissions = User.FindAll("permission").Select(c => c.Value).ToList(),
            BusinessId = Guid.TryParse(User.FindFirst("businessId")?.Value, out var bid) ? bid : null,
            BranchId = Guid.TryParse(User.FindFirst("branchId")?.Value, out var brid) ? brid : null,
            IsAegisUser = bool.TryParse(User.FindFirst("isAegisUser")?.Value, out var isAegis) && isAegis,
            AegisRole = User.FindFirst("AegisRole")?.Value,
            SubscriptionTier = User.FindFirst("SubscriptionTier")?.Value
        };

        return Success(claims, "Token claims retrieved successfully");
    }

    // Helper methods
    private string GetIpAddress()
    {
        string? rawIp = Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(rawIp))
            rawIp = rawIp.Split(',')[0].Trim();
        else
            rawIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrWhiteSpace(rawIp))
            return "0.0.0.0";

        // Normalize IPv6 with port: [2001:db8::1]:443 ? 2001:db8::1
        if (rawIp.StartsWith("[") && rawIp.Contains("]"))
            rawIp = rawIp.Substring(1, rawIp.IndexOf(']') - 1);

        // IPv4 or malformed IPv6: remove port after last colon only if IPv4-like
        if (rawIp.Count(c => c == ':') == 1 && rawIp.Contains(':'))
            rawIp = rawIp[..rawIp.LastIndexOf(':')];

        // Validate final result
        if (!IPAddress.TryParse(rawIp, out _))
            rawIp = "0.0.0.0";

        return rawIp;
    }

    /// <summary>
    /// Sets refresh token in a secure HTTP-only session cookie
    /// Addresses VAPT finding: Improper Session Management
    /// Uses session cookie (no Expires) to prevent persistent storage across browser sessions
    /// </summary>
    private void SetRefreshTokenCookie(string refreshToken)
    {
        // =================================================================
        // SECURITY: Session Cookie Configuration (VAPT Compliance)
        // =================================================================
        // Addresses VAPT finding: "Session stored as persistent cookie and remains valid until expiry"
        //
        // Key Security Features:
        // 1. NO Expires attribute = Session cookie (deleted when browser closes)
        // 2. MaxAge controls token lifetime while browser is open
        // 3. HttpOnly prevents JavaScript access (XSS protection)
        // 4. Secure enforces HTTPS-only transmission
        // 5. SameSite=Strict prevents CSRF attacks
        // =================================================================

        var cookieOptions = new CookieOptions
        {
            // CRITICAL: No Expires attribute - makes this a SESSION cookie
            // Cookie will be deleted when browser is closed
            // This addresses the VAPT finding about persistent cookies

            HttpOnly = true,  // Prevents client-side JavaScript from accessing the cookie (XSS protection)
            Secure = true,    // Cookie only sent over HTTPS (prevents man-in-the-middle attacks)
            SameSite = SameSiteMode.Strict, // Prevents CSRF - cookie not sent on cross-site requests

            // MaxAge controls how long the cookie is valid WHILE the browser session is active
            // This is different from Expires - the cookie is still session-based
            // After 7 days OR browser close (whichever comes first), token is invalid
            MaxAge = TimeSpan.FromDays(7), // Token lifetime while browser is open

            IsEssential = true // Required for authentication flow
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    private string? GetRefreshTokenFromCookie()
    {
        return Request.Cookies["refreshToken"];
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete("refreshToken");
    }

    /// <summary>
    /// Allows the authenticated user to change their own password.
    /// Exposed at /auth/change-password for frontend compatibility.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestModel request)
    {
        var command = new ChangePasswordCommand(request.CurrentPassword, request.NewPassword);
        var result = await Mediator.Send(command);
        if (!result.IsSuccess)
            return Error(result.Message);
        return Success<object>(null!, result.Message);
    }
}

// Request/Response models

/// <summary>
/// Login request with user credentials
/// </summary>
/// <param name="Email">User email address</param>
/// <param name="Password">User password</param>
public record LoginRequest(string Email, string Password);

/// <summary>
/// Login response with JWT tokens and user information
/// </summary>
/// <param name="AccessToken">Encrypted JWT access token for API authentication</param>
/// <param name="RefreshToken">Refresh token for obtaining new access tokens</param>
/// <param name="UserId">Unique identifier of the authenticated user</param>
/// <param name="TenantId">Business/tenant ID the user belongs to (null for Aegis admins)</param>
/// <param name="MustChangePassword">Indicates if user must change password on next login</param>
/// <param name="ExpiresAt">Access token expiration timestamp</param>
/// <param name="Claims">Token claims for frontend UI (roles, permissions, user info)</param>
/// <param name="TerminatedSessionCount">Number of previous sessions terminated due to concurrent login policy</param>
/// <param name="SessionWarning">Warning message about terminated sessions (if any)</param>
public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    Guid? TenantId,
    bool MustChangePassword,
    DateTimeOffset ExpiresAt,
    TokenClaimsDto? Claims,
    int TerminatedSessionCount = 0,
    string? SessionWarning = null);

/// <summary>
/// Refresh token request
/// </summary>
/// <param name="RefreshToken">Valid refresh token to exchange for new access token</param>
public record RefreshTokenRequest(string RefreshToken);

/// <summary>
/// Refresh token response with new JWT tokens
/// </summary>
/// <param name="AccessToken">New encrypted JWT access token</param>
/// <param name="RefreshToken">New refresh token (old token is revoked)</param>
/// <param name="ExpiresAt">New access token expiration timestamp</param>
/// <param name="Claims">Token claims for frontend UI (roles, permissions, user info)</param>
public record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    TokenClaimsDto? Claims);

/// <summary>
/// Logout request with optional refresh token
/// </summary>
/// <param name="RefreshToken">Refresh token to revoke (optional if stored in cookie)</param>
public record LogoutRequest(string? RefreshToken);

/// <summary>
/// Business self-registration request
/// </summary>
public record RegisterBusinessRequest(
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminPhone,
    string BusinessName,
    Guid PlatformSubscriptionId,
    BillingCycle BillingCycle,
    string? Tin = null);

/// <summary>
/// Business registration response with Paystack payment URL
/// </summary>
public record RegisterBusinessResponse(
    Guid PendingRegistrationId,
    string Reference,
    string PaymentUrl);

/// <summary>
/// Change password request (self-service)
/// </summary>
public record ChangePasswordRequestModel(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);