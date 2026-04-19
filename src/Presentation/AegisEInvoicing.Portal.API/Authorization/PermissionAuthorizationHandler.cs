using Microsoft.AspNetCore.Authorization;

namespace AegisEInvoicing.Portal.API.Authorization;

/// <summary>
/// Evaluates <see cref="PermissionRequirement"/> by checking whether the authenticated
/// user's JWT carries the required "permission" claim.
/// AegisAdmins are automatically granted all permissions.
/// </summary>
public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Platform admins bypass all permission checks
        if (context.User.IsInRole(Domain.Constants.RoleConstants.AegisAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var hasPermission = context.User
            .FindAll("permission")
            .Any(c => string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
