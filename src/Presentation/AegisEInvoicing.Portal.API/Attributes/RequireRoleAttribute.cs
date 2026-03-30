using AegisEInvoicing.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace AegisEInvoicing.Portal.API.Attributes;

/// <summary>
/// Authorization attribute that requires specific roles for accessing endpoints
/// Supports multiple roles with OR logic (user needs at least one of the specified roles)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireRoleAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new instance of RequireRoleAttribute with specific roles
    /// </summary>
    /// <param name="roles">One or more roles required to access the endpoint</param>
    public RequireRoleAttribute(params string[] roles)
    {
        if (roles == null || roles.Length == 0)
            throw new ArgumentException("At least one role must be specified", nameof(roles));

        Roles = string.Join(",", roles);
    }
}

/// <summary>
/// Authorization attribute that requires specific permissions for accessing endpoints
/// Supports multiple permissions with OR logic (user needs at least one of the specified permissions)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new instance of RequirePermissionAttribute with specific permissions
    /// </summary>
    /// <param name="permissions">One or more permissions required to access the endpoint</param>
    public RequirePermissionAttribute(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified", nameof(permissions));

        // Store permissions in policy for authorization handlers to use
        Policy = $"RequirePermissions:{string.Join(",", permissions)}";
    }
}

/// <summary>
/// Authorization attribute that requires the user to be a merchant admin
/// This is a convenience attribute for the most common admin check in SaaS structure
/// </summary>
public class RequireClientAdminAttribute : RequireRoleAttribute
{
    public RequireClientAdminAttribute() : base(RoleConstants.ClientAdmin)
    {
    }
}

/// <summary>
/// Authorization attribute that requires the user to be a platform admin (KMPG user)
/// This is for KMPG-only operations that require platform-level access
/// </summary>
public class RequireAegisAdminAttribute : RequireRoleAttribute
{
    public RequireAegisAdminAttribute() : base(RoleConstants.AegisAdmin)
    {
    }
}