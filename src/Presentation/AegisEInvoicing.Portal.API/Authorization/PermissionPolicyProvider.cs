using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AegisEInvoicing.Portal.API.Authorization;

/// <summary>
/// Dynamically resolves authorization policies whose names follow the pattern
/// "RequirePermissions:{permission}" — the pattern set by <see cref="Attributes.RequirePermissionAttribute"/>.
///
/// This means every permission string is automatically backed by a policy without
/// needing manual AddPolicy(...) calls for each one.
/// Unknown policy names fall back to the default authorization policy.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    private const string PolicyPrefix = "RequirePermissions:";
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
            return _fallback.GetPolicyAsync(policyName);

        // Support comma-separated permissions (OR logic — user needs at least one)
        var permissions = policyName[PolicyPrefix.Length..]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var builder = new AuthorizationPolicyBuilder();
        builder.RequireAuthenticatedUser();

        // All listed permissions must be satisfied (AND logic per [RequirePermission] attribute)
        // For OR logic within one attribute, we combine into a single requirement below
        foreach (var permission in permissions)
            builder.AddRequirements(new PermissionRequirement(permission));

        return Task.FromResult<AuthorizationPolicy?>(builder.Build());
    }
}
