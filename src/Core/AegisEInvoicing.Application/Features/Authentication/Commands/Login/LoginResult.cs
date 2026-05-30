using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.Authentication.Commands.Login;

/// <summary>
/// DTO containing token claims for frontend consumption
/// </summary>
public record TokenClaimsDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public Guid? BusinessId { get; set; }
    public Guid? BranchId { get; set; }
    public bool IsAegisUser { get; set; }
    public string? AegisRole { get; set; }
    public string? SubscriptionTier { get; set; }
    public string? DeploymentMode { get; set; }
    public bool MustChangePassword { get; set; }
}

public record LoginResult : GenericResult
{
    public string? AccessToken { get; set; } = null;
    public string? RefreshToken { get; set; } = null;
    public Guid? UserId { get; set; } = null;
    public Guid? BusinessId { get; set; } = null;
    public bool MustChangePassword { get; set; } = false;
    public DateTimeOffset? ExpiresAt { get; set; } = null;

    /// <summary>
    /// Token claims for frontend UI (roles, permissions, user info)
    /// </summary>
    public TokenClaimsDto? Claims { get; set; } = null;

    /// <summary>
    /// Number of previous sessions that were terminated due to concurrent session limit
    /// Addresses VAPT finding: Concurrent login enabled
    /// </summary>
    public int TerminatedSessionCount { get; set; } = 0;

    /// <summary>
    /// Warning message about terminated sessions (if any)
    /// Provides user notification about concurrent login enforcement
    /// </summary>
    public string? SessionWarning { get; set; } = null;

    public static LoginResult Successful(string accessToken, string refreshToken, Guid userId,
                                      Guid? businessId, bool mustChangePassword, DateTimeOffset expiresAt,
                                      TokenClaimsDto? claims = null, int terminatedSessionCount = 0)
    {
        var result = new LoginResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.LOGIN_SUCCESSFUL,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = userId,
            BusinessId = businessId,
            MustChangePassword = mustChangePassword,
            ExpiresAt = expiresAt,
            Claims = claims,
            TerminatedSessionCount = terminatedSessionCount
        };

        // Add warning message if previous sessions were terminated
        // This addresses VAPT finding: "generate an alert message when the second user tries to log in"
        if (terminatedSessionCount > 0)
        {
            result.SessionWarning = terminatedSessionCount == 1
                ? "Your previous session on another device has been terminated due to the concurrent login policy. Only one active session is allowed at a time."
                : $"Your previous {terminatedSessionCount} sessions on other devices have been terminated due to the concurrent login policy. Only one active session is allowed at a time.";
        }

        return result;
    }

    public new static LoginResult Failure(string? message = null)
    {
        return new LoginResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Unauthorized.ToInt(),
            Message = message ?? ResponseMessages.LOGIN_FAILURE
        };
    }

    public static LoginResult Locked(string message)
    {
        return new LoginResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Locked.ToInt(),
            Message = message
        };
    }
}
