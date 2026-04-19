using Microsoft.AspNetCore.Authorization;

namespace AegisEInvoicing.Portal.API.Authorization;

/// <summary>
/// Authorization requirement that demands the caller holds a specific permission claim.
/// Used by <see cref="PermissionAuthorizationHandler"/> and resolved dynamically by
/// <see cref="PermissionPolicyProvider"/> so no manual AddPolicy registration is needed.
/// </summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
