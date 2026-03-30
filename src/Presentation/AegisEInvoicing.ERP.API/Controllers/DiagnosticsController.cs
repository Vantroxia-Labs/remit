using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.ERP.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AegisEInvoicing.ERP.API.Controllers;

/// <summary>
/// Diagnostics controller for debugging authentication and user context
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class DiagnosticsController : BaseApiController
{
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        ICurrentUserService currentUser,
        ILogger<DiagnosticsController> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Get current user context information
    /// </summary>
    /// <returns>User context details</returns>
    [HttpGet("current-user")]
    public IActionResult GetCurrentUser()
    {
        try
        {
            var result = new
            {
                // ICurrentUserService properties
                CurrentUserService = new
                {
                    UserId = _currentUser.UserId,
                    UserName = _currentUser.UserName,
                    Email = _currentUser.Email,
                    IsAuthenticated = _currentUser.IsAuthenticated,
                    BusinessId = _currentUser.BusinessId,
                    BranchId = _currentUser.BranchId,
                    IsBusinessLevel = _currentUser.IsBusinessLevel,
                    IsBranchLevel = _currentUser.IsBranchLevel,
                    IsPlatformAdmin = _currentUser.IsPlatformAdmin,
                    IsAegisUser = _currentUser.IsAegisUser,
                    AegisRole = _currentUser.AegisRole,
                    AegisEmployeeId = _currentUser.AegisEmployeeId,
                    AegisDepartment = _currentUser.AegisDepartment,
                    Roles = _currentUser.Roles?.ToList(),
                    Permissions = _currentUser.Permissions?.ToList()
                },

                // Raw claims from HttpContext
                Claims = User.Claims.Select(c => new
                {
                    Type = c.Type,
                    Value = c.Value
                }).ToList(),

                // HttpContext.Items
                HttpContextItems = new
                {
                    BusinessId = HttpContext.Items["BusinessId"]?.ToString(),
                    RateLimitTier = HttpContext.Items["RateLimitTier"]?.ToString()
                },

                // Authentication info
                Authentication = new
                {
                    IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                    AuthenticationType = User.Identity?.AuthenticationType,
                    Name = User.Identity?.Name,
                    Schemes = HttpContext.Request.Headers["Authorization"].ToString()
                }
            };

            _logger.LogInformation("Diagnostics endpoint called - UserId: {UserId}, BusinessId: {BusinessId}",
                _currentUser.UserId, _currentUser.BusinessId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "User context retrieved successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user context");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get all claims from current principal
    /// </summary>
    /// <returns>List of claims</returns>
    [HttpGet("claims")]
    public IActionResult GetClaims()
    {
        var claims = User.Claims.Select(c => new
        {
            Type = c.Type,
            Value = c.Value,
            Issuer = c.Issuer,
            OriginalIssuer = c.OriginalIssuer,
            ValueType = c.ValueType
        }).ToList();

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = $"Found {claims.Count} claims",
            Data = new
            {
                TotalClaims = claims.Count,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                AuthenticationType = User.Identity?.AuthenticationType,
                Claims = claims
            }
        });
    }

    /// <summary>
    /// Test endpoint to verify API Key authentication is working
    /// </summary>
    /// <returns>Authentication status</returns>
    [HttpGet("auth-status")]
    public IActionResult GetAuthStatus()
    {
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Authentication successful",
            Data = new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                AuthenticationType = User.Identity?.AuthenticationType,
                Name = User.Identity?.Name,
                ClaimsCount = User.Claims.Count(),
                HasBusinessId = _currentUser.BusinessId.HasValue,
                HasUserId = _currentUser.UserId.HasValue
            }
        });
    }
}
