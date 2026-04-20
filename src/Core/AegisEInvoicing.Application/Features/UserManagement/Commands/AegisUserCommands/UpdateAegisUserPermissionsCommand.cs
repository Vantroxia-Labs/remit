using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.UserManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.AegisUserCommands;

/// <summary>
/// Replaces the permission set of an Aegis staff user's custom scoped platform role.
/// Passing an empty list grants full AegisAdmin access (no custom role restriction).
/// </summary>
public record UpdateAegisUserPermissionsCommand(
    Guid UserId,
    List<string> Permissions) : IRequest<UpdateAegisUserPermissionsResult>;

public record UpdateAegisUserPermissionsResult(bool IsSuccess, string Message)
{
    public static UpdateAegisUserPermissionsResult Success(string message) => new(true, message);
    public static UpdateAegisUserPermissionsResult Failure(string message) => new(false, message);
}

public class UpdateAegisUserPermissionsCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<UpdateAegisUserPermissionsCommand, UpdateAegisUserPermissionsResult>
{
    public async Task<UpdateAegisUserPermissionsResult> Handle(
        UpdateAegisUserPermissionsCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == null)
            return UpdateAegisUserPermissionsResult.Failure("Authentication required");

        if (!currentUser.IsAegisUser || !currentUser.HasRole(RoleConstants.AegisAdmin))
            return UpdateAegisUserPermissionsResult.Failure("Only Aegis Platform Admins can update staff permissions");

        var user = await context.Users
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsAegisUser, cancellationToken);

        if (user == null)
            return UpdateAegisUserPermissionsResult.Failure("Aegis user not found");

        var validPermissions = request.Permissions
            .Where(p => PermissionConstants.AegisAdminAssignablePermissions.Contains(p))
            .Distinct()
            .ToList();

        // Find existing custom role for this user (named AegisStaff_{userId:N})
        var customRoleName = $"AegisStaff_{request.UserId:N}";
        var existingCustomRole = await context.PlatformRoles
            .FirstOrDefaultAsync(r => r.Name == customRoleName && !r.IsDeleted, cancellationToken);

        if (validPermissions.Count == 0)
        {
            // No restrictions — remove custom role so user falls back to full AegisAdmin access
            if (existingCustomRole != null)
            {
                existingCustomRole.MarkAsDeleted(currentUser.UserId.Value);
                await context.SaveChangesAsync(cancellationToken);
            }
            return UpdateAegisUserPermissionsResult.Success("Permissions reset to full AegisAdmin access");
        }

        if (existingCustomRole != null)
        {
            // Diff and apply changes to the existing custom role
            var toRemove = existingCustomRole.Permissions
                .Except(validPermissions, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var toAdd = validPermissions
                .Except(existingCustomRole.Permissions, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var p in toRemove)
                existingCustomRole.RemovePermission(p);
            foreach (var p in toAdd)
                existingCustomRole.AddPermission(p);

            await context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Create a new custom role
            var customRole = PlatformRole.Create(
                name: customRoleName,
                description: $"Custom permissions for Aegis staff member {user.Email}",
                category: "AegisStaff",
                sortOrder: 99,
                createdBy: currentUser.UserId.Value);

            foreach (var p in validPermissions)
                customRole.AddPermission(p);

            await context.PlatformRoles.AddAsync(customRole, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            user.AssignRole(customRole.Id, currentUser.UserId.Value);
            await context.SaveChangesAsync(cancellationToken);
        }

        return UpdateAegisUserPermissionsResult.Success("Permissions updated successfully");
    }
}
