using Ardalis.GuardClauses;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AegisEInvoicing.Infrastructure.Services;

/// <summary>
/// Service for accessing current user information from HTTP context in SaaS platform
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Property to always get the current user from HttpContext (not cached)
    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // Use TryParse to safely handle invalid input (VAPT finding: time-based SQL injection prevention)
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? UserName => User?.FindFirst(ClaimTypes.Name)?.Value;

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? BusinessId
    {
        get
        {
            var businessIdClaim = User?.FindFirst("businessId")?.Value;
            // Use TryParse to safely handle invalid input (VAPT finding: time-based SQL injection prevention)
            return Guid.TryParse(businessIdClaim, out var businessId) ? businessId : null;
        }
    }

    public Guid? BranchId
    {
        get
        {
            var branchIdClaim = User?.FindFirst("branchId")?.Value;
            // Use TryParse to safely handle invalid input (VAPT finding: time-based SQL injection prevention)
            return Guid.TryParse(branchIdClaim, out var branchId) ? branchId : null;
        }
    }

    // Use TryParse for safe boolean conversion (VAPT finding: prevent exceptions from malformed claims)
    public bool IsBusinessLevel => bool.TryParse(User?.FindFirst("isBusinessLevel")?.Value, out var result) && result;

    public bool IsBranchLevel => bool.TryParse(User?.FindFirst("isBranchLevel")?.Value, out var result) && result;

    public bool IsPlatformAdmin => HasRole(RoleConstants.AegisAdmin);

    // KMPG-specific properties - Use TryParse for safe boolean conversion
    public bool IsAegisUser => bool.TryParse(User?.FindFirst("isAegisUser")?.Value, out var result) && result;

    public string? AegisRole => User?.FindFirst("AegisRole")?.Value;

    public string? AegisEmployeeId => User?.FindFirst("AegisEmployeeId")?.Value;

    public string? AegisDepartment => User?.FindFirst("AegisDepartment")?.Value;

    public IEnumerable<string> Roles => User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value) ?? [];

    public IEnumerable<string> Permissions => User?.FindAll("permission")
        .Select(c => c.Value) ?? [];

    public bool HasRole(string role)
    {
        Guard.Against.NullOrWhiteSpace(role, nameof(role));
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public bool HasPermission(string permission)
    {
        Guard.Against.NullOrWhiteSpace(permission, nameof(permission));
        return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}