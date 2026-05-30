using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.RoleManagement.Commands.UpdateBusinessRolePermissions;

/// <summary>
/// Replaces the full permission set of a business-scoped custom role.
/// The caller supplies the desired final list; the handler diffs and applies Add/Remove.
/// System roles (BusinessId = null) and roles owned by another business are rejected.
/// </summary>
public record UpdateBusinessRolePermissionsCommand(
    Guid RoleId,
    IReadOnlyList<string> Permissions) : IRequest;

public class UpdateBusinessRolePermissionsCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateBusinessRolePermissionsCommand>
{
    public async Task Handle(
        UpdateBusinessRolePermissionsCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.BusinessId.HasValue)
            throw new AuthenticationException("Business context is required.");

        if (!currentUser.HasRole(RoleConstants.ClientAdmin))
            throw new ForbiddenException("Only ClientAdmins can update business role permissions.");

        var businessId = currentUser.BusinessId.Value;

        var role = await context.PlatformRoles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException($"Role {request.RoleId} not found.");

        if (role.IsSystemRole || role.BusinessId == null)
            throw new ForbiddenException("System roles cannot be modified.");

        if (role.BusinessId != businessId)
            throw new ForbiddenException("You can only modify roles belonging to your business.");

        // Validate against allowed permission set
        var invalid = request.Permissions
            .Except(PermissionConstants.ClientAdminAssignablePermissions, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (invalid.Count > 0)
            throw new BadRequestException(
                $"The following permissions are not assignable by a ClientAdmin: {string.Join(", ", invalid)}");

        // Apply diff: remove permissions not in new list, add new ones
        var toRemove = role.Permissions
            .Except(request.Permissions, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var toAdd = request.Permissions
            .Except(role.Permissions, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var p in toRemove)
            role.RemovePermission(p);

        foreach (var p in toAdd)
            role.AddPermission(p);

        await context.SaveChangesAsync(cancellationToken);
    }
}
