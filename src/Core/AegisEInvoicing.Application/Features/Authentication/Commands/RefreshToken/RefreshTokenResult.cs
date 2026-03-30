using AegisEInvoicing.Application.Features.Authentication.Commands.Login;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.Authentication.Commands.RefreshToken;

public record RefreshTokenResult : GenericResult
{
    public string? AccessToken { get; set; } = null;
    public string? RefreshToken { get; set; } = null;
    public DateTimeOffset? ExpiresAt { get; set; } = null;

    /// <summary>
    /// Token claims for frontend UI (roles, permissions, user info)
    /// </summary>
    public TokenClaimsDto? Claims { get; set; } = null;

    public static RefreshTokenResult Successful(
        string accessToken,
        string refreshToken,
        DateTimeOffset expiresAt,
        TokenClaimsDto? claims = null,
        string message = "Token refreshed successfully")
    {
        return new RefreshTokenResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            Claims = claims,
            Message = message,
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt()
        };
    }

    public static new RefreshTokenResult AuthorizationError(string? message = null)
    {
        return new RefreshTokenResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message ?? "Authorization failed"
        };
    }

    public static new RefreshTokenResult Failure(string? message = null)
    {
        return new RefreshTokenResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.ExpectationFailed.ToInt(),
            Message = message ?? "Token refresh failed"
        };
    }
}