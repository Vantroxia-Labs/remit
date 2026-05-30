using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;

using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.RoleManagement.Commands.CreateBusinessRole;

/// <summary>
/// Creates a custom, business-scoped role whose permission set is chosen by
/// the ClientAdmin from the allowed list in <see cref="PermissionConstants.ClientAdminAssignablePermissions"/>.
/// System roles (BusinessId = null) are never touched by this command.
/// </summary>
public record CreateBusinessRoleCommand(
    string Name,
    string Description,
    IReadOnlyList<string> Permissions) : IRequest<CreateBusinessRoleResult>;

public record CreateBusinessRoleResult(Guid RoleId, string Name);

public class CreateBusinessRoleCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateBusinessRoleCommand, CreateBusinessRoleResult>
{
    public async Task<CreateBusinessRoleResult> Handle(
        CreateBusinessRoleCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.BusinessId.HasValue)
            throw new AuthenticationException("Business context is required.");

        if (!currentUser.HasRole(RoleConstants.ClientAdmin))
            throw new ForbiddenException("Only ClientAdmins can create business roles.");

        var businessId = currentUser.BusinessId.Value;

        // Validate all requested permissions are within the allowed set
        var invalid = request.Permissions
            .Except(PermissionConstants.ClientAdminAssignablePermissions, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (invalid.Count > 0)
            throw new BadRequestException(
                $"The following permissions are not assignable by a ClientAdmin: {string.Join(", ", invalid)}");

        // Prevent duplicate name within the same business
        var nameExists = await context.PlatformRoles
            .AnyAsync(r => !r.IsDeleted
                        && r.BusinessId == businessId
                        && r.Name == request.Name,
                      cancellationToken);

        if (nameExists)
            throw new ConflictException($"A custom role named '{request.Name}' already exists for your business.");

        var role = PlatformRole.CreateBusinessRole(
            name: request.Name,
            description: request.Description,
            businessId: businessId,
            createdBy: currentUser.UserId!.Value,
            permissions: request.Permissions);

        context.PlatformRoles.Add(role);
        await context.SaveChangesAsync(cancellationToken);

        return new CreateBusinessRoleResult(role.Id, role.Name);
    }
}
