using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.RoleManagement.Commands.DeleteBusinessRole;

/// <summary>
/// Soft-deletes a custom business role. System roles (BusinessId == null) are never deleted.
/// Fails if the role has active user assignments.
/// </summary>
public record DeleteBusinessRoleCommand(Guid RoleId) : IRequest<DeleteBusinessRoleResult>;

public record DeleteBusinessRoleResult(string Message);

public class DeleteBusinessRoleCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<DeleteBusinessRoleCommand, DeleteBusinessRoleResult>
{
    public async Task<DeleteBusinessRoleResult> Handle(
        DeleteBusinessRoleCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.BusinessId.HasValue)
            throw new AuthenticationException("Business context is required.");

        if (!currentUser.HasRole(RoleConstants.ClientAdmin))
            throw new ForbiddenException("Only ClientAdmins can delete business roles.");

        var businessId = currentUser.BusinessId.Value;

        var role = await context.PlatformRoles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException($"Role '{request.RoleId}' not found.");

        if (role.BusinessId == null)
            throw new ForbiddenException("System roles cannot be deleted.");

        if (role.BusinessId != businessId)
            throw new ForbiddenException("You can only delete roles belonging to your own business.");

        var activeAssignments = await context.UserRoleAssignments
            .CountAsync(a => a.PlatformRoleId == request.RoleId && a.IsActive, cancellationToken);

        if (activeAssignments > 0)
            throw new BadRequestException(
                $"This role is currently assigned to {activeAssignments} user(s). Reassign them before deleting.");

        role.MarkAsDeleted(currentUser.UserId);
        await context.SaveChangesAsync(cancellationToken);

        return new DeleteBusinessRoleResult($"Role '{role.Name}' deleted successfully.");
    }
}
